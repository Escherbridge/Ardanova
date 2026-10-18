using Microsoft.AspNetCore.Mvc;

namespace ArdaNova.API.Filters;

public static class ArdaNovaMvcPolicies
{
    /// <summary>Order that puts request validation after every authorization filter, whatever its scope.</summary>
    public const int AfterAuthorizationFilters = 100;

    /// <summary>Registers the endpoint authorization convention and the global paging, validation, disclosure and error-envelope filters.</summary>
    public static MvcOptions AddArdaNovaEndpointPolicies(this MvcOptions options)
    {
        options.Conventions.Add(new EndpointAuthorizationConvention());
        options.Filters.Add<PagingArgumentFilter>();
        options.Filters.Add<RequestValidationFilter>(AfterAuthorizationFilters);
        options.Filters.Add<UserDirectoryPrivacyFilter>();
        options.Filters.Add<EventVisibilityFilter>();
        options.Filters.Add<ReferralDisclosureFilter>();
        options.Filters.Add<ErrorEnvelopeResultFilter>();
        return options;
    }
}
