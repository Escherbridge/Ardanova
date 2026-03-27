namespace ArdaNova.Application.Settings;

public class KycSettings
{
    public string Provider { get; set; } = "manual";
    public string? VeriffApiKey { get; set; }
    public string? VeriffBaseUrl { get; set; } = "https://stationapi.veriff.com/v1";
    public int SubmissionExpiryDays { get; set; } = 0; // 0 = never expires
}
