namespace ArdaNova.Application.Validators;

using System.Text.RegularExpressions;

/// <summary>Shared field checks used by the request validators.</summary>
public static partial class FieldRules
{
    public const int NameLength = 200;
    public const int EmailLength = 254;
    public const int PhoneLength = 50;
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

    /// <summary>A profile handle (no scheme) or an http(s) URL; control characters are refused because browsers strip them out of href schemes.</summary>
    public static void HandleOrWebUrl(this ValidationErrors errors, string field, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        var trimmed = value.Trim();
        var namesScheme = SchemePrefix().IsMatch(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal);
        errors.When(
            value.Length > ShortTextLength || value.Any(char.IsControl) || (namesScheme && !IsWebUrl(trimmed)),
            field,
            $"{field} must be a handle or an http or https URL.");
    }

    /// <summary>One plain address: something@domain.tld, no spaces, at most 254 characters.</summary>
    public static void Email(this ValidationErrors errors, string field, string? value, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.When(required, field, $"{field} is required.");
            return;
        }
        errors.When(value.Length > EmailLength || !EmailShape().IsMatch(value), field, $"{field} must be a valid email address.");
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

    [GeneratedRegex("^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase)]
    private static partial Regex SchemePrefix();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailShape();
}
