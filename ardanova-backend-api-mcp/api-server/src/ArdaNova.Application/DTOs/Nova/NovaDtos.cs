namespace ArdaNova.Application.DTOs.Nova;

/// <summary>One question Nova AI asked and the member's answer; questions round-trip byte-identical.</summary>
public sealed class NovaTurnDto
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
}

/// <summary>A project-refinement turn: the idea, every answered question, and the opaque state of the last one.</summary>
public sealed class NovaRefineRequestDto
{
    public string Idea { get; init; } = string.Empty;
    public List<NovaTurnDto> Turns { get; init; } = [];
    public string? State { get; init; }
    public string? SessionId { get; init; }
}

/// <summary>Nova AI's reply: the next question (with state to send back) or the feasibility verdict.</summary>
public sealed class NovaRefineResponseDto
{
    /// <summary>"question" or "verdict".</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Always "Nova AI"; set here, never taken from upstream.</summary>
    public string Assistant { get; init; } = NovaBrand.AssistantName;
    /// <summary>"live" (answered by the Nova AI service) or "local-preview" (fixed practice questions; nothing analysed).</summary>
    public string Mode { get; init; } = NovaModes.Live;
    public string? Question { get; init; }
    public int? Number { get; init; }
    public int? MaxQuestions { get; init; }
    public string? State { get; init; }
    public string? SessionId { get; init; }
    public NovaVerdictDto? Verdict { get; init; }
    public string? Vertical { get; init; }
    public NovaBriefDto? Brief { get; init; }
}

/// <summary>The feasibility verdict: go, adjust or not_yet, with the evidence it relied on.</summary>
public sealed class NovaVerdictDto
{
    public string Outcome { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public List<string> Strengths { get; init; } = [];
    public List<string> Risks { get; init; } = [];
    public string NextStep { get; init; } = string.Empty;
    public List<NovaSourceDto> Sources { get; init; } = [];
}

/// <summary>A public web source the verdict cited.</summary>
public sealed class NovaSourceDto
{
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

/// <summary>The refined project brief a go/adjust verdict carries.</summary>
public sealed class NovaBriefDto
{
    public string WorkingName { get; init; } = string.Empty;
    public string OneLiner { get; init; } = string.Empty;
    public string Customer { get; init; } = string.Empty;
    public string Problem { get; init; } = string.Empty;
    public string Offer { get; init; } = string.Empty;
    public string Pricing { get; init; } = string.Empty;
    public List<string> Channels { get; init; } = [];
    public List<string> Milestones { get; init; } = [];
    public List<string> Metrics { get; init; } = [];
}

/// <summary>Values for <see cref="NovaRefineResponseDto.Mode"/>.</summary>
public static class NovaModes
{
    public const string Live = "live";
    public const string LocalPreview = "local-preview";
}

/// <summary>The in-product brand; the upstream provider's name never reaches a member.</summary>
public static class NovaBrand
{
    public const string AssistantName = "Nova AI";

    /// <summary>Upstream names that must never appear in a member-facing reply (see Infrastructure/Ogai/AGENTS.md).</summary>
    public static readonly string[] HiddenNames = ["overseasgenai", "overseas genai", "overseas gen ai"];
}

/// <summary>One upstream refinement turn as the partner API returns it; mapped to <see cref="NovaRefineResponseDto"/>.</summary>
public sealed class OgaiTurn
{
    public string Type { get; init; } = string.Empty;
    public string? Question { get; init; }
    public int? Number { get; init; }
    public int? MaxQuestions { get; init; }
    public string? State { get; init; }
    public string? SessionId { get; init; }
    public NovaVerdictDto? Verdict { get; init; }
    public string? Vertical { get; init; }
    public NovaBriefDto? Brief { get; init; }
    /// <summary>True when the turn came from the local preview client, not the Nova AI service.</summary>
    public bool LocalPreview { get; init; }
}
