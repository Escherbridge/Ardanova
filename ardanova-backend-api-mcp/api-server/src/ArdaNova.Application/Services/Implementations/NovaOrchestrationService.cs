namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs.Nova;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>Nova AI project refinement over the upstream validator. See Infrastructure/Ogai/AGENTS.md.</summary>
public sealed class NovaOrchestrationService : INovaOrchestrationService
{
    /// <summary>Mirrors the upstream form rules so a refused input never spends a session or a model call.</summary>
    public const int MinIdeaLength = 20;
    public const int MaxIdeaLength = 2000;
    public const int MaxAnswerLength = 1500;
    public const int MaxQuestionLength = 400;
    public const int MaxTurns = 8;

    /// <summary>Error codes the controller maps to HTTP; the member-facing copy lives in the client.</summary>
    public const string UnavailableError = "nova_unavailable";
    public const string QuotaError = "nova_daily_limit";
    public const string SessionNotFoundError = "nova_session_not_found";

    private readonly IOgaiClient _client;
    private readonly NovaSessionAllowance _allowance;
    private readonly ILogger<NovaOrchestrationService> _logger;

    public NovaOrchestrationService(IOgaiClient client, NovaSessionAllowance allowance, ILogger<NovaOrchestrationService> logger)
    {
        _client = client;
        _allowance = allowance;
        _logger = logger;
    }

    public async Task<Result<NovaRefineResponseDto>> RefineProjectIdeaAsync(
        NovaRefineRequestDto request,
        string actorId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            return Result<NovaRefineResponseDto>.Unauthorized("An authenticated member is required.");

        var idea = (request.Idea ?? string.Empty).Trim();
        if (idea.Length is < MinIdeaLength or > MaxIdeaLength)
            return Result<NovaRefineResponseDto>.ValidationError(
                $"Describe the project idea in {MinIdeaLength} to {MaxIdeaLength} characters.");

        var rawTurns = request.Turns ?? [];
        if (rawTurns.Count > MaxTurns)
            return Result<NovaRefineResponseDto>.ValidationError($"A refinement has at most {MaxTurns} questions.");

        var turns = new List<NovaTurnDto>(rawTurns.Count);
        foreach (var turn in rawTurns)
        {
            var answer = (turn.Answer ?? string.Empty).Trim();
            if (answer.Length is 0 or > MaxAnswerLength || string.IsNullOrEmpty(turn.Question) || turn.Question.Length > MaxQuestionLength)
                return Result<NovaRefineResponseDto>.ValidationError(
                    $"Answer each question in 1 to {MaxAnswerLength} characters.");
            turns.Add(new NovaTurnDto { Question = turn.Question, Answer = answer });
        }

        var firstTurn = turns.Count == 0;
        if (!firstTurn && (string.IsNullOrWhiteSpace(request.State) || string.IsNullOrWhiteSpace(request.SessionId)))
            return Result<NovaRefineResponseDto>.ValidationError("Continue the refinement with the state Nova AI returned.");

        // A continuation must name a session this actor started; anyone else's id reads as absent.
        if (!firstTurn && !_allowance.OwnsSession(actorId, request.SessionId!))
            return Result<NovaRefineResponseDto>.NotFound(SessionNotFoundError);

        if (firstTurn && !_allowance.TryStart(actorId))
            return Result<NovaRefineResponseDto>.Conflict(QuotaError);

        var upstream = await _client.ValidateAsync(idea, turns, firstTurn ? null : request.State, firstTurn ? null : request.SessionId, ct);
        if (upstream.IsFailure || upstream.Value is null)
        {
            if (firstTurn)
                _allowance.Refund(actorId);
            return upstream.Type switch
            {
                ResultType.ValidationError => Result<NovaRefineResponseDto>.ValidationError(upstream.Error ?? "Nova AI could not use that input."),
                ResultType.Conflict => Result<NovaRefineResponseDto>.Conflict(QuotaError),
                _ => Result<NovaRefineResponseDto>.Failure(UnavailableError),
            };
        }

        var turnResult = upstream.Value;
        if (turnResult.Type is not ("question" or "verdict") || (turnResult.Type == "verdict" && turnResult.Verdict is null))
        {
            _logger.LogWarning("Nova upstream returned an unusable turn {Type}", turnResult.Type);
            return Result<NovaRefineResponseDto>.Failure(UnavailableError);
        }

        // Defense in depth: the partner API already hides its own name; a slip is dropped, never shown.
        if (NamesUpstream(turnResult))
        {
            _logger.LogWarning("Nova upstream reply named the provider; dropped");
            return Result<NovaRefineResponseDto>.Failure(UnavailableError);
        }

        if (!string.IsNullOrWhiteSpace(turnResult.SessionId))
            _allowance.BindSession(actorId, turnResult.SessionId);

        return Result<NovaRefineResponseDto>.Success(new NovaRefineResponseDto
        {
            Type = turnResult.Type,
            Assistant = NovaBrand.AssistantName,
            Mode = turnResult.LocalPreview ? NovaModes.LocalPreview : NovaModes.Live,
            Question = turnResult.Question,
            Number = turnResult.Number,
            MaxQuestions = turnResult.MaxQuestions,
            State = turnResult.State,
            SessionId = turnResult.SessionId,
            Verdict = turnResult.Verdict,
            Vertical = turnResult.Vertical,
            Brief = turnResult.Brief,
        });
    }

    /// <summary>True when any member-visible text of the turn contains a hidden upstream name.</summary>
    private static bool NamesUpstream(OgaiTurn turn)
    {
        var visible = new List<string?> { turn.Question };
        if (turn.Verdict is { } verdict)
        {
            visible.AddRange([verdict.Headline, verdict.Summary, verdict.NextStep]);
            visible.AddRange(verdict.Strengths);
            visible.AddRange(verdict.Risks);
            visible.AddRange(verdict.Sources.SelectMany(source => new[] { source.Title, source.Url }));
        }
        if (turn.Brief is { } brief)
        {
            visible.AddRange([brief.WorkingName, brief.OneLiner, brief.Customer, brief.Problem, brief.Offer, brief.Pricing]);
            visible.AddRange(brief.Channels.Concat(brief.Milestones).Concat(brief.Metrics));
        }
        return visible.Any(text => text is not null &&
            NovaBrand.HiddenNames.Any(name => text.Contains(name, StringComparison.OrdinalIgnoreCase)));
    }
}
