using Microsoft.AspNetCore.Mvc;

namespace ArdaNova.API.Filters;

public static class ArdaNovaMvcPolicies
{
    /// <summary>Registers the endpoint authorization convention and the global paging, privacy and error-envelope filters.</summary>
    public static MvcOptions AddArdaNovaEndpointPolicies(this MvcOptions options)
    {
        options.Conventions.Add(new EndpointAuthorizationConvention());
        options.Filters.Add<PagingArgumentFilter>();
        options.Filters.Add<UserDirectoryPrivacyFilter>();
        options.Filters.Add<ErrorEnvelopeResultFilter>();
        return options;
    }
}
