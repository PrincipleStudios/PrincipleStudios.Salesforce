using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrincipleStudios.Salesforce;

/// <summary>
/// A Salesforce client with full functionality
/// </summary>
public interface ISalesforceClient : ISalesforceHttpClient, ISalesforceQueryClient, ISalesforceSearchClient
{
}

/// <summary>
/// A Salesforce HTTP client.
/// </summary>
public interface ISalesforceHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

public interface ISalesforceQueryClient
{
    /// <summary>
    /// Issues a SOQL query.
    /// </summary>
    /// <typeparam name="T">The type of each record</typeparam>
    /// <param name="query">The SOQL query as a FormattableString.</param>
    /// <param name="options">Options for the SOQL query. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The parsed response from Salesforce. <seealso cref="QueryResponse<>"/></returns>
    Task<QueryResponse<T>> QueryAsync<T>(FormattableString query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the next page of data from a SOQL query.
    /// </summary>
    /// <typeparam name="T">The type of each record</typeparam>
    /// <param name="query">The response from the previous request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The parsed response from Salesforce. <seealso cref="QueryResponse<>"/></returns>
    Task<QueryResponse<T>> NextAsync<T>(QueryResponse<T> query, CancellationToken cancellationToken = default);
}

public interface ISalesforceSearchClient
{
    /// <summary>
    /// Issues a SOSL search.
    /// </summary>
    /// <typeparam name="T">The type of each record</typeparam>
    /// <param name="query">The SOSL query.</param>
    /// <param name="options">Options for the SOQL query. Optional.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The parsed response from Salesforce. <seealso cref="SearchResponse<>"/></returns>
    Task<SearchResponse<T>> SearchAsync<T>(FormattableString query, SalesforceRequestOptions options = default, CancellationToken cancellationToken = default);
}

/// <summary>
/// A set of options for customizing how SOQL and SOSL queries are issued.
/// </summary>
public record struct SalesforceRequestOptions
{
    /// <summary>
    /// Overrides the API version for the request. If omitted, uses the default
    /// for the <see cref="ISalesforceHttpClient"/>.
    /// 
    /// Should include the `v`, such as `v58.0`.
    /// </summary>
    public string? ApiVersion { get; init; }
    /// <summary>
    /// If true, the query will not have additional whitespace trimmed before
    /// sending to Salesforce. Otherwise, extra whitespace will be collapsed.
    /// </summary>
    public bool SkipTrim { get; init; }
    /// <summary>
    /// If provided, the JSON Serializer options to use for the response
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; init; }
}

/// <summary>
/// Response type for SOQL queries
/// </summary>
/// <typeparam name="T">The type of each record</typeparam>
/// <seealso href="https://developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/dome_query.htm"/>
public class QueryResponse<T>
{
    [JsonPropertyName("nextRecordsUrl")]
    public Uri? NextRecordsUrl { get; init; }

    [JsonPropertyName("totalSize")]
    public int TotalSize { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("records")]
    public required IEnumerable<T> Records { get; init; }
}

/// <summary>
/// Response type for SOSL searches
/// </summary>
/// <typeparam name="T">The type of each record</typeparam>
/// <seealso href="https://developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/dome_search.htm"/>
public class SearchResponse<T>
{
    [JsonPropertyName("searchRecords")]
    public required IEnumerable<T> Records { get; init; }
}
