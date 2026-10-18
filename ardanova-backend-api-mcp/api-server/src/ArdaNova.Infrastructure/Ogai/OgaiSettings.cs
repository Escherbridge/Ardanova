namespace ArdaNova.Infrastructure.Ogai;

/// <summary>Configuration section <c>Ogai</c> (env <c>Ogai__BaseUrl</c>, <c>Ogai__PartnerApiKey</c>); see AGENTS.md.</summary>
public sealed class OgaiSettings
{
    public const string SectionName = "Ogai";

    /// <summary>Partner API origin, e.g. https://overseasgenai.example; empty selects the local preview (or disabled in Production).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Raw partner API key (secret store only); sent as a bearer token, never logged.</summary>
    public string PartnerApiKey { get; set; } = string.Empty;

    /// <summary>Per-turn timeout: covers the upstream's retry-and-fallback budget (~88 s) and stays under the BFF's Nova timeout (100 s). See AGENTS.md §Turn budget.</summary>
    public int TimeoutSeconds { get; set; } = 95;

    /// <summary>True when both the origin and the key are set.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(PartnerApiKey);
}
