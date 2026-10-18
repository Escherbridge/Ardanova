namespace ArdaNova.Application.Validators;

using ArdaNova.Application.DTOs;

/// <summary>Shared event field rules; capacity is a positive head count.</summary>
internal static class EventFieldRules
{
    public const int MaximumAttendees = 100_000;

    public static void Capacity(ValidationErrors errors, int? maxAttendees)
        => errors.When(maxAttendees is <= 0 or > MaximumAttendees, "MaxAttendees", $"MaxAttendees must be between 1 and {MaximumAttendees}.");

    public static void Dates(ValidationErrors errors, DateTime? start, DateTime? end)
        => errors.When(start is not null && end is not null && end < start, "EndDate", "EndDate must not be before StartDate.");
}

public sealed class CreateEventValidator : RequestValidator<CreateEventDto>
{
    protected override void Check(CreateEventDto request, ValidationErrors errors)
    {
        errors.RequiredText(nameof(request.Title), request.Title, FieldRules.NameLength);
        errors.OptionalText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Location), request.Location, FieldRules.ShortTextLength);
        errors.WebUrl(nameof(request.LocationUrl), request.LocationUrl);
        errors.WebUrl(nameof(request.MeetingUrl), request.MeetingUrl);
        errors.WebUrlOrPath(nameof(request.CoverImage), request.CoverImage);
        errors.When(string.IsNullOrWhiteSpace(request.Timezone), nameof(request.Timezone), "Timezone is required.");
        errors.Timezone(nameof(request.Timezone), request.Timezone);
        EventFieldRules.Capacity(errors, request.MaxAttendees);
        EventFieldRules.Dates(errors, request.StartDate, request.EndDate);
    }
}

/// <summary>Checks supplied fields only; a lone start or end date is ordered against the stored one in EventService.UpdateAsync.</summary>
public sealed class UpdateEventValidator : RequestValidator<UpdateEventDto>
{
    protected override void Check(UpdateEventDto request, ValidationErrors errors)
    {
        errors.OptionalText(nameof(request.Title), request.Title, FieldRules.NameLength);
        errors.When(request.Title is { Length: 0 }, nameof(request.Title), "Title cannot be empty.");
        errors.OptionalText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Location), request.Location, FieldRules.ShortTextLength);
        errors.WebUrl(nameof(request.LocationUrl), request.LocationUrl);
        errors.WebUrl(nameof(request.MeetingUrl), request.MeetingUrl);
        errors.WebUrlOrPath(nameof(request.CoverImage), request.CoverImage);
        errors.Timezone(nameof(request.Timezone), request.Timezone);
        EventFieldRules.Capacity(errors, request.MaxAttendees);
        EventFieldRules.Dates(errors, request.StartDate, request.EndDate);
    }
}

public sealed class RegisterEventValidator : RequestValidator<RegisterEventDto>
{
    protected override void Check(RegisterEventDto request, ValidationErrors errors)
        => errors.OptionalText(nameof(request.Notes), request.Notes, FieldRules.ShortTextLength);
}
