namespace ArdaNova.Infrastructure.Azoa;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;

/// <summary>
/// Infrastructure adapter that satisfies the Application-layer port
/// <see cref="IAzoaAvatarNode"/> by delegating to the typed-HttpClient transport
/// <see cref="IAzoaPublicNodeClient"/>. Mirrors the <c>IAlgorandService</c> seam:
/// the interface lives in Application, the implementation here in Infrastructure,
/// so the Application layer never references Infrastructure wire models.
/// </summary>
public sealed class AzoaAvatarNodeAdapter : IAzoaAvatarNode
{
    private readonly IAzoaPublicNodeClient _node;

    public AzoaAvatarNodeAdapter(IAzoaPublicNodeClient node)
    {
        _node = node;
    }

    public async Task<Result<AzoaAvatarRef>> RegisterAvatarAsync(
        AzoaAvatarRegistration registration, CancellationToken ct = default)
    {
        var wire = new AzoaAvatarRegisterRequest
        {
            Username = registration.Username,
            Email = registration.Email,
            Password = registration.Password,
            Title = registration.Title,
            FirstName = registration.FirstName,
            LastName = registration.LastName,
        };

        var result = await _node.RegisterAvatarAsync(wire, ct);

        // Preserve the failure type verbatim (incl. KYC_FORBIDDEN → Forbidden).
        if (result.IsFailure)
            return MapFailure<AzoaAvatarRef>(result);

        var avatar = result.Value!;
        return Result<AzoaAvatarRef>.Success(
            new AzoaAvatarRef(avatar.Id.ToString(), avatar.Username, avatar.Email));
    }

    /// <summary>Re-wrap a failed Result&lt;TIn&gt; as Result&lt;TOut&gt;, keeping the ResultType.</summary>
    private static Result<TOut> MapFailure<TOut>(Result<AzoaAvatar> source) => source.Type switch
    {
        ResultType.NotFound => Result<TOut>.NotFound(source.Error!),
        ResultType.Forbidden => Result<TOut>.Forbidden(source.Error!),
        ResultType.Unauthorized => Result<TOut>.Unauthorized(source.Error!),
        ResultType.Conflict => Result<TOut>.Conflict(source.Error!),
        ResultType.ValidationError => Result<TOut>.ValidationError(source.Error!),
        ResultType.BadRequest => Result<TOut>.BadRequest(source.Error!),
        _ => Result<TOut>.Failure(source.Error ?? "AZOA avatar registration failed."),
    };
}
