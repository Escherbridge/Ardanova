namespace ArdaNova.Application.Validators;

/// <summary>Shared field checks used by the request validators.</summary>
public static class FieldRules
{
    public const int NameLength = 200;
    public const int ShortTextLength = 500;
    public const int LongTextLength = 20_000;
    public const int UrlLength = 2_048;
    public const decimal MaximumMoney = 1_000_000_000_000m;

    /// <summary>Abbreviations the event form offers that are not IANA ids on every host.</summary>
    private static readonly HashSet<string> FormTimezones = new(StringComparer.Ordinal)
    {
        "UTC", "EST", "PST", "CET", "IST", "JST",
    };

    public static void RequiredText(this ValidationErrors errors, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(field, $"{field} is required.");
        else
            errors.MaxLength(field, value, maxLength);
    }

    /// <summary>Checks only a supplied value; null means "unchanged" on updates.</summary>
    public static void OptionalText(this ValidationErrors errors, string field, string? value, int maxLength)
    {
        if (value is null)
            return;
        if (value.Length > 0 && string.IsNullOrWhiteSpace(value))
            errors.Add(field, $"{field} cannot be blank.");
        errors.MaxLength(field, value, maxLength);
    }

    public static void MaxLength(this ValidationErrors errors, string field, string value, int maxLength)
        => errors.When(value.Length > maxLength, field, $"{field} must be at most {maxLength} characters.");

    /// <summary>An absolute http(s) URL with a host; blocks javascript:, data: and friends.</summary>
    public static void WebUrl(this ValidationErrors errors, string field, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        errors.When(value.Length > UrlLength || !IsWebUrl(value), field, $"{field} must be an http or https URL.");
    }

    /// <summary>An absolute http(s) URL, or a site-relative path such as an uploaded image's public path.</summary>
    public static void WebUrlOrPath(this ValidationErrors errors, string field, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        var isPath = value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.Contains('\\');
        errors.When(value.Length > UrlLength || !(isPath || IsWebUrl(value)), field, $"{field} must be an http or https URL or a site path.");
    }

    public static void Timezone(this ValidationErrors errors, string field, string? value)
    {
        if (value is null)
            return;
        errors.When(!IsKnownTimezone(value), field, $"{field} must be a known time zone.");
    }

    public static void Money(this ValidationErrors errors, string field, decimal? value)
    {
        if (value is null)
            return;
        errors.When(value < 0 || value > MaximumMoney, field, $"{field} must be between 0 and {MaximumMoney}.");
    }

    public static bool IsWebUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrEmpty(uri.Host);

    public static bool IsKnownTimezone(string value)
        => value.Length is > 0 and <= 64
            && (FormTimezones.Contains(value) || TimeZoneInfo.TryFindSystemTimeZoneById(value, out _));
}
