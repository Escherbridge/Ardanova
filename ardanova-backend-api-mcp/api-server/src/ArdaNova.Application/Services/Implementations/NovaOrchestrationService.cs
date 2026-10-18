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
    public const string UnavailableError = NovaErrors.Unavailable;
    public const string QuotaError = NovaErrors.DailyLimit;
    public const string CapacityError = NovaErrors.Capacity;
    public const string SessionNotFoundError = NovaErrors.SessionNotFound;
    public const string TurnInFlightError = NovaErrors.TurnInFlight;

    private readonly IOgaiClient _client;
    private readonly NovaSessionAllowance _allowance;
    private readonly NovaSessionTickets _tickets;
    private readonly ILogger<NovaOrchestrationService> _logger;

    public NovaOrchestrationService(IOgaiClient client, NovaSessionAllowance allowance, NovaSessionTickets tickets, ILogger<NovaOrchestrationService> logger)
    {
        _client = client;
        _allowance = allowance;
        _tickets = tickets;
        _logger = logger;
    }

    public NovaStatusDto GetStatus(string actorId)
    {
        var counted = CountsAllowance && !string.IsNullOrWhiteSpace(actorId);
        return new()
        {
            Refinement = _client.Availability,
            DailyLimit = counted ? _allowance.DailyLimit : null,
            RemainingToday = counted ? _allowance.Remaining(actorId) : null,
        };
    }

    /// <summary>Only the live service spends the member's daily allowance; the preview's fixed questions cost nothing.</summary>
    private bool CountsAllowance => _client.Availability == NovaAvailability.Live;

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

        // A continuation must carry this actor's live ticket for this session; anyone else's, or an expired one, reads as absent.
        string? upstreamState = null;
        var expiresAt = _tickets.NewExpiry();
        if (!firstTurn && !_tickets.TryOpen(request.State, actorId, request.SessionId!, out upstreamState, out expiresAt))
            return Result<NovaRefineResponseDto>.NotFound(SessionNotFoundError);

        // One turn at a time per member, so no account can hold several of the partner's shared model-call slots.
        if (!_allowance.TryBeginTurn(actorId))
            return Result<NovaRefineResponseDto>.Conflict(TurnInFlightError);
        try
        {
            return await RunTurnAsync(idea, turns, upstreamState, firstTurn ? null : request.SessionId, actorId, expiresAt, ct);
        }
        finally
        {
            _allowance.EndTurn(actorId);
        }
    }

    /// <summary>Spends the allowance on a first live turn, calls upstream and maps its reply; the caller holds the turn slot.</summary>
    private async Task<Result<NovaRefineResponseDto>> RunTurnAsync(
        string idea,
        List<NovaTurnDto> turns,
        string? upstreamState,
        string? sessionId,
        string actorId,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        var firstTurn = turns.Count == 0;
        var spendsAllowance = firstTurn && CountsAllowance;
        if (spendsAllowance && !_allowance.TryStart(actorId))
            return Result<NovaRefineResponseDto>.Conflict(QuotaError);

        var upstream = await _client.ValidateAsync(idea, turns, upstreamState, sessionId, ct);
        if (upstream.IsFailure || upstream.Value is null)
        {
            if (spendsAllowance)
                _allowance.Refund(actorId);
            return upstream.Type switch
            {
                ResultType.ValidationError => Result<NovaRefineResponseDto>.ValidationError(upstream.Error ?? "Nova AI could not use that input."),
                ResultType.NotFound => Result<NovaRefineResponseDto>.NotFound(SessionNotFoundError),
                _ when upstream.Error == CapacityError => Result<NovaRefineResponseDto>.Failure(CapacityError),
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

        // A question whose state cannot fit a ticket the client will send back is a dead end; say so now, not next turn.
        if (turnResult.Type == "question" && turnResult.State is { Length: > NovaSessionTickets.MaxUpstreamStateLength })
        {
            _logger.LogWarning("Nova upstream state of {Length} characters exceeds the ticket limit", turnResult.State.Length);
            return Result<NovaRefineResponseDto>.Failure(UnavailableError);
        }

        var ticket = string.IsNullOrWhiteSpace(turnResult.SessionId) || string.IsNullOrWhiteSpace(turnResult.State)
            ? null
            : _tickets.Issue(actorId, turnResult.SessionId, turnResult.State, expiresAt);

        return Result<NovaRefineResponseDto>.Success(new NovaRefineResponseDto
        {
            Type = turnResult.Type,
            Assistant = NovaBrand.AssistantName,
            Mode = turnResult.LocalPreview ? NovaModes.LocalPreview : NovaModes.Live,
            Question = turnResult.Question,
            Number = turnResult.Number,
            MaxQuestions = turnResult.MaxQuestions,
            State = ticket,
            SessionId = turnResult.SessionId,
            Verdict = turnResult.Verdict,
            Vertical = turnResult.Vertical,
            Brief = turnResult.Brief,
            RemainingToday = CountsAllowance ? _allowance.Remaining(actorId) : null,
        });
    }

    /// <summary>True when any member-visible text of the turn contains a hidden upstream name.</summary>
    private static bool NamesUpstream(OgaiTurn turn)
    {
        var visible = new List<string?> { turn.Question, turn.Vertical };
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
        return visible.Any(NovaBrand.NamesUpstream);
    }
}
