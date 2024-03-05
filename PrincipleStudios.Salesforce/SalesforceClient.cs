using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PrincipleStudios.Salesforce;

public class SalesforceClient : ISalesforceClient, IUnsafeSalesforceClient
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
        return await QueryImplementation<T>(q.FinalQuery, options, cancellationToken).ConfigureAwait(false);
    }

    public Task<QueryResponse<T>> UnsafeQueryAsync<T>(string query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default) =>
        QueryImplementation<T>(query, options, cancellationToken);

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

        var q = query.ToSoslQuery(options.SkipTrim);
        logger.LogSoslSearch(q.FinalQuery, q.Format);
        return await SearchImplementation<T>(q.FinalQuery, options, cancellationToken).ConfigureAwait(false);
    }

    public Task<SearchResponse<T>> UnsafeSearchAsync<T>(string query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default) =>
        SearchImplementation<T>(query, options, cancellationToken);

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

    private async Task<QueryResponse<T>> QueryImplementation<T>(string query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default)
    {
        var fullUrl = new Uri(instanceUrl, $"services/data/{options.ApiVersion ?? apiVersion}/query/?q={Uri.EscapeDataString(query)}");

        using var response = await httpClient.GetAsync(fullUrl, cancellationToken).ConfigureAwait(false);
        var result = await DeserializeFromResponseStreamAsync<QueryResponse<T>>(response, options.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<SearchResponse<T>> SearchImplementation<T>(string query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default)
    {
        var fullUrl = new Uri(instanceUrl, $"services/data/{options.ApiVersion ?? apiVersion}/search/?q={Uri.EscapeDataString(query)}");

        using var response = await httpClient.GetAsync(fullUrl, cancellationToken).ConfigureAwait(false);
        var result = await DeserializeFromResponseStreamAsync<SearchResponse<T>>(response, options.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }
}
