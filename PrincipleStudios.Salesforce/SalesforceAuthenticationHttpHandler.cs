namespace PrincipleStudios.Salesforce;

/// <summary>
/// An HttpMessageHandler to assist with authenticating with Salesforce.
/// </summary>
public abstract class SalesforceAuthenticationHttpHandler : DelegatingHandler
{
    protected SalesforceAuthenticationHttpHandler() : base() { }
    protected SalesforceAuthenticationHttpHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var accessToken = await GetAccessTokenAsync().ConfigureAwait(false);
        request.Headers.Authorization = CreateSalesforceAuthorizationHeader(accessToken);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(true);
        if (response.StatusCode is not System.Net.HttpStatusCode.Unauthorized)
            return response;

        accessToken = await GetNewAccessTokenAsync().ConfigureAwait(false);
        if (accessToken == null)
        {
            // No new access token provided; return not authorized
            return response;
        }

        // since we're not passing on the response, no one else can dispose it
        response.Dispose();
        request.Headers.Authorization = CreateSalesforceAuthorizationHeader(accessToken);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static System.Net.Http.Headers.AuthenticationHeaderValue CreateSalesforceAuthorizationHeader(string accessToken) =>
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            scheme: "Bearer",
            parameter: accessToken
        );

    /// <summary>
    /// Gets the initial access token for the request
    /// </summary>
    /// <returns>A Salesforce access token</returns>
    protected abstract ValueTask<string> GetAccessTokenAsync();
    /// <summary>
    /// Gets a new access token for the request. Called if `GetAccessTokenAsync` previously failed. 
    /// </summary>
    /// <returns>A new access token to retry the request, otherwise null.</returns>
    protected virtual ValueTask<string?> GetNewAccessTokenAsync() =>
#if NET5_0_OR_GREATER
        ValueTask.FromResult<string?>(null);
#else
        new (Task.FromResult<string?>(null));
#endif
}
