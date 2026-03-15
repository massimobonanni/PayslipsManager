using Azure.Core;
using Microsoft.Identity.Web;

namespace PayslipsManager.Infrastructure.Services;

/// <summary>
/// A <see cref="TokenCredential"/> that acquires tokens on behalf of the signed-in user
/// via Microsoft Identity Web's <see cref="ITokenAcquisition"/>.
/// This allows the Azure SDK (e.g. BlobServiceClient) to authenticate as the current user,
/// so Azure RBAC policies on individual containers are enforced per-user.
/// </summary>
internal sealed class UserDelegatedTokenCredential : TokenCredential
{
    // Use explicit scopes matching what was consented during sign-in.
    // The Azure SDK passes "https://storage.azure.com/.default" which may not
    // match cached tokens acquired for "user_impersonation" during OIDC flow.
    private static readonly string[] StorageScopes = ["https://storage.azure.com/user_impersonation"];

    private readonly ITokenAcquisition _tokenAcquisition;

    public UserDelegatedTokenCredential(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition ?? throw new ArgumentNullException(nameof(tokenAcquisition));
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
            StorageScopes, tokenAcquisitionOptions: new TokenAcquisitionOptions
            {
                CancellationToken = cancellationToken
            });

        return new AccessToken(result.AccessToken, result.ExpiresOn);
    }
}
