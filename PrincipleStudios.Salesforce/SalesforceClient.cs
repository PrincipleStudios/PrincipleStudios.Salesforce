using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PrincipleStudios.Salesforce;

public class SalesforceClient : ISalesforceClient
{
    private readonly string apiVersion;
    private readonly HttpClient httpClient;
    private readonly Uri instanceUrl;
    private readonly ILogger logger;

    public SalesforceClient(HttpClient httpClient, Uri instanceUrl, string apiVersion, ILogger<SalesforceClient> logger)
    {
        this.apiVersion = apiVersion ?? throw new ArgumentNullException(nameof(apiVersion));
        this.logger = logger;
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.instanceUrl = instanceUrl ?? throw new ArgumentNullException(nameof(instanceUrl));
    }

    public async Task<QueryResponse<T>> QueryAsync<T>(FormattableString query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var q = query.ToSoqlQuery(options.SkipTrim);
        logger.LogSoqlQuery(q.FinalQuery, q.Format);
        var fullUrl = new Uri(instanceUrl, $"services/data/{options.ApiVersion ?? apiVersion}/query/?q={Uri.EscapeDataString(q.FinalQuery)}");

        using var response = await httpClient.GetAsync(fullUrl, cancellationToken).ConfigureAwait(false);
        var result = await DeserializeFromResponseStreamAsync<QueryResponse<T>>(response, options.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<QueryResponse<T>> NextAsync<T>(QueryResponse<T> previous, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default)
    {
        if (previous.NextRecordsUrl is null)
            return QueryResponse.Empty<T>();

        logger.LogSoqlNextPage(previous.NextRecordsUrl);

        using var response = await httpClient.GetAsync(previous.NextRecordsUrl, cancellationToken).ConfigureAwait(false);
        var result = await DeserializeFromResponseStreamAsync<QueryResponse<T>>(response, options.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<SearchResponse<T>> SearchAsync<T>(FormattableString query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var q = query.ToSoqlQuery(options.SkipTrim);
        var fullUrl = new Uri(instanceUrl, $"services/data/{options.ApiVersion ?? apiVersion}/search/?q={Uri.EscapeDataString(q.FinalQuery)}");
        logger.LogSoslSearch(q.FinalQuery, q.Format);

        using var response = await httpClient.GetAsync(fullUrl, cancellationToken).ConfigureAwait(false);
        var result = await DeserializeFromResponseStreamAsync<SearchResponse<T>>(response, options.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return httpClient.SendAsync(request, cancellationToken);
    }

    private static async Task<T?> DeserializeFromResponseStreamAsync<T>(HttpResponseMessage responseMessage, JsonSerializerOptions? serializerOptions, CancellationToken cancellationToken)
    {
        responseMessage.EnsureSuccessStatusCode();

        using var responseStream =
#if NET5_0_OR_GREATER
            await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        return await JsonSerializer.DeserializeAsync<T>(responseStream, serializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
