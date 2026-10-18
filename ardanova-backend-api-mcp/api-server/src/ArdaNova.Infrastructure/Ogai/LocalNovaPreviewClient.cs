namespace ArdaNova.Infrastructure.Ogai;

using System.Security.Cryptography;
using System.Text;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs.Nova;
using ArdaNova.Application.Services.Interfaces;

/// <summary>Deterministic stand-in used outside Production when Ogai is unconfigured: fixed questions, no assessment.</summary>
public sealed class LocalNovaPreviewClient : IOgaiClient
{
    public const string PreviewOutcome = "preview";

    public string Availability => NovaAvailability.LocalPreview;

    private static readonly string[] Questions =
    [
        "Who has this problem, specifically: what kind of person or organisation, and where?",
        "How do they deal with it today, and what does that cost them in money or time?",
        "How will you reach your first 20 contributors or customers?",
    ];

    public Task<Result<OgaiTurn>> ValidateAsync(
        string idea,
        IReadOnlyList<NovaTurnDto> turns,
        string? state,
        string? sessionId,
        CancellationToken ct = default)
    {
        var expectedSession = SessionFor(idea);
        if (turns.Count > 0 && (sessionId != expectedSession || state != StateFor(turns.Count - 1)))
            return Task.FromResult(Result<OgaiTurn>.NotFound("That refinement is out of date. Start again."));

        if (turns.Count < Questions.Length)
        {
            return Task.FromResult(Result<OgaiTurn>.Success(new OgaiTurn
            {
                Type = "question",
                Question = Questions[turns.Count],
                Number = turns.Count + 1,
                MaxQuestions = Questions.Length,
                State = StateFor(turns.Count),
                SessionId = expectedSession,
                LocalPreview = true,
            }));
        }

        return Task.FromResult(Result<OgaiTurn>.Success(new OgaiTurn
        {
            Type = "verdict",
            SessionId = expectedSession,
            LocalPreview = true,
            Verdict = new NovaVerdictDto
            {
                Outcome = PreviewOutcome,
                Headline = "Local preview: no assessment was made.",
                Summary = "This environment is not connected to the Nova AI service, so these were fixed practice questions and nothing you wrote was analysed.",
                NextStep = "Connect Nova AI in this environment, then run the refinement again for a real feasibility verdict.",
            },
        }));
    }

    private static string StateFor(int questionIndex) => $"local-preview:{questionIndex}";

    private static string SessionFor(string idea)
        => "LOCALPREVIEW" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idea)))[..14];
}

/// <summary>Production without Ogai configuration: Nova AI is off rather than silently faked.</summary>
public sealed class DisabledOgaiClient : IOgaiClient
{
    public string Availability => NovaAvailability.Off;

    public Task<Result<OgaiTurn>> ValidateAsync(
        string idea,
        IReadOnlyList<NovaTurnDto> turns,
        string? state,
        string? sessionId,
        CancellationToken ct = default)
        => Task.FromResult(Result<OgaiTurn>.Failure(NovaUpstreamErrors.Unavailable));
}
