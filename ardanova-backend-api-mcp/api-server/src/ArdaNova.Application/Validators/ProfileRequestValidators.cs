namespace ArdaNova.Application.Validators;

using ArdaNova.Application.DTOs;

public sealed class CreateGuildValidator : RequestValidator<CreateGuildDto>
{
    protected override void Check(CreateGuildDto request, ValidationErrors errors)
    {
        errors.RequiredText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.RequiredText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.Email(nameof(request.Email), request.Email, required: true);
        errors.WebUrl(nameof(request.Website), request.Website);
        errors.WebUrlOrPath(nameof(request.Logo), request.Logo);
        errors.OptionalText(nameof(request.Phone), request.Phone, FieldRules.PhoneLength);
        errors.OptionalText(nameof(request.Address), request.Address, FieldRules.ShortTextLength);
        errors.OptionalText(nameof(request.Portfolio), request.Portfolio, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Specialties), request.Specialties, FieldRules.LongTextLength);
    }
}

public sealed class UpdateGuildValidator : RequestValidator<UpdateGuildDto>
{
    protected override void Check(UpdateGuildDto request, ValidationErrors errors)
    {
        errors.OptionalText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.When(request.Name is { Length: 0 }, nameof(request.Name), "Name cannot be empty.");
        errors.OptionalText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.Email(nameof(request.Email), request.Email, required: false);
        errors.WebUrl(nameof(request.Website), request.Website);
        errors.WebUrlOrPath(nameof(request.Logo), request.Logo);
        errors.OptionalText(nameof(request.Phone), request.Phone, FieldRules.PhoneLength);
        errors.OptionalText(nameof(request.Address), request.Address, FieldRules.ShortTextLength);
        errors.OptionalText(nameof(request.Portfolio), request.Portfolio, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Specialties), request.Specialties, FieldRules.LongTextLength);
    }
}

public sealed class UpdateUserValidator : RequestValidator<UpdateUserDto>
{
    protected override void Check(UpdateUserDto request, ValidationErrors errors)
    {
        errors.OptionalText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.OptionalText(nameof(request.Bio), request.Bio, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Location), request.Location, FieldRules.ShortTextLength);
        errors.OptionalText(nameof(request.Phone), request.Phone, FieldRules.PhoneLength);
        errors.WebUrl(nameof(request.Website), request.Website);
        errors.HandleOrWebUrl(nameof(request.LinkedIn), request.LinkedIn);
        errors.HandleOrWebUrl(nameof(request.Twitter), request.Twitter);
        errors.WebUrlOrPath(nameof(request.Image), request.Image);
    }
}

public sealed class CreateGroupConversationValidator : RequestValidator<CreateGroupConversationDto>
{
    protected override void Check(CreateGroupConversationDto request, ValidationErrors errors)
    {
        errors.RequiredText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.WebUrlOrPath(nameof(request.AvatarUrl), request.AvatarUrl);
    }
}

public sealed class UpdateGroupConversationValidator : RequestValidator<UpdateGroupConversationDto>
{
    protected override void Check(UpdateGroupConversationDto request, ValidationErrors errors)
    {
        errors.OptionalText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.WebUrlOrPath(nameof(request.AvatarUrl), request.AvatarUrl);
    }
}

public sealed class CreateNotificationValidator : RequestValidator<CreateNotificationDto>
{
    protected override void Check(CreateNotificationDto request, ValidationErrors errors)
    {
        errors.RequiredText(nameof(request.Title), request.Title, FieldRules.ShortTextLength);
        errors.RequiredText(nameof(request.Message), request.Message, FieldRules.LongTextLength);
        errors.WebUrlOrPath(nameof(request.ActionUrl), request.ActionUrl);
    }
}
