using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrincipleStudios.Salesforce;

public abstract class MockableHttpMessageHandler : DelegatingHandler
{
    private static readonly Regex salesforcePath = new Regex("^/services/data/v[0-9.]+/(?:search|query)/\\?q=(?<soql>.+)$", RegexOptions.Compiled);
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = Send(request);
        if (response == null)
        {
            if (MatchSalesforcePath(request.RequestUri?.PathAndQuery) is { Success: true, Groups: var groups })
            {
                var soql = Uri.UnescapeDataString(groups["soql"].Value);
                // SOQL mock not set up
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                // other query
                var content = request.Content?.ReadAsStringAsync(cancellationToken).Result;
                System.Diagnostics.Debugger.Break();
            }
            throw new System.InvalidOperationException("Must set up mocks for HTTP calls");
        }
        return response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Send(request, CancellationToken.None));
    }

    public abstract HttpResponseMessage Send(HttpRequestMessage request);

    public static Match MatchSalesforcePath(string? pathAndQuery) => salesforcePath.Match(pathAndQuery ?? "");
}
