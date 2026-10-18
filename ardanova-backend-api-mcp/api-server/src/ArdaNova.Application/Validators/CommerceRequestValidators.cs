namespace ArdaNova.Application.Validators;

using System.Text.RegularExpressions;
using ArdaNova.Application.DTOs;

public sealed class CreateProductValidator : RequestValidator<CreateProductDto>
{
    protected override void Check(CreateProductDto request, ValidationErrors errors)
    {
        errors.RequiredText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.OptionalText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Sku), request.Sku, 100);
        errors.OptionalText(nameof(request.Category), request.Category, 100);
        errors.Money(nameof(request.Price), request.Price);
        errors.Money(nameof(request.Cost), request.Cost);
    }
}

public sealed class UpdateProductValidator : RequestValidator<UpdateProductDto>
{
    protected override void Check(UpdateProductDto request, ValidationErrors errors)
    {
        errors.OptionalText(nameof(request.Name), request.Name, FieldRules.NameLength);
        errors.When(request.Name is { Length: 0 }, nameof(request.Name), "Name cannot be empty.");
        errors.OptionalText(nameof(request.Description), request.Description, FieldRules.LongTextLength);
        errors.OptionalText(nameof(request.Sku), request.Sku, 100);
        errors.OptionalText(nameof(request.Category), request.Category, 100);
        errors.Money(nameof(request.Price), request.Price);
        errors.Money(nameof(request.Cost), request.Cost);
    }
}

/// <summary>A referral names two different people and, when given, a URL-safe code.</summary>
public sealed partial class CreateReferralValidator : RequestValidator<CreateReferralDto>
{
    protected override void Check(CreateReferralDto request, ValidationErrors errors)
    {
        errors.When(
            string.Equals(request.ReferrerId, request.ReferredId, StringComparison.Ordinal),
            nameof(request.ReferredId),
            "A user cannot refer themselves.");
        errors.When(
            request.ReferralCode is not null && !ReferralCode().IsMatch(request.ReferralCode),
            nameof(request.ReferralCode),
            "ReferralCode must be 4-64 letters, digits, '-' or '_'.");
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{4,64}$")]
    private static partial Regex ReferralCode();
}
