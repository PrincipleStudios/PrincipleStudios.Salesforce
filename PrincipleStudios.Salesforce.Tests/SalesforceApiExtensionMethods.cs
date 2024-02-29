using Moq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace PrincipleStudios.Salesforce;

public static class SalesforceApiExtensionMethods
{
    private static readonly Regex soqlTrim = new Regex(@"\s+", RegexOptions.Compiled);

    public static Moq.Language.Flow.ISetup<MockableHttpMessageHandler, HttpResponseMessage> SetupSalesforceQuery(this Mock<MockableHttpMessageHandler> target, FormattableString query, string apiVersion, bool skipTrim = false)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrEmpty(apiVersion))
        {
            throw new ArgumentException($"'{nameof(apiVersion)}' cannot be null or empty.", nameof(apiVersion));
        }

        return target.Setup(query.ToSalesforceQueryCallExpression(apiVersion, skipTrim));
    }

    public static Moq.Language.Flow.ISetup<MockableHttpMessageHandler, HttpResponseMessage> SetupSalesforceSearch(this Mock<MockableHttpMessageHandler> target, FormattableString query, string apiVersion, bool skipTrim = false)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrEmpty(apiVersion))
        {
            throw new ArgumentException($"'{nameof(apiVersion)}' cannot be null or empty.", nameof(apiVersion));
        }

        return target.Setup(query.ToSalesforceSearchCallExpression(apiVersion, skipTrim));
    }

    public static Expression<Func<MockableHttpMessageHandler, HttpResponseMessage>> ToSalesforceQueryCallExpression(this FormattableString query, string apiVersion, bool skipTrim = false)
    {
        var finalQuery = Uri.EscapeDataString(query.ToSoqlQuery(skipTrim).FinalQuery);
        var expectedPath = $"/services/data/{apiVersion}/query/?q={finalQuery}";
        return m => m.Send(It.Is<HttpRequestMessage>(req => ValidRequest(req, skipTrim, expectedPath)));
    }

    public static Expression<Func<MockableHttpMessageHandler, HttpResponseMessage>> ToSalesforceSearchCallExpression(this FormattableString query, string apiVersion, bool skipTrim = false)
    {
        var finalQuery = Uri.EscapeDataString(query.ToSoqlQuery(skipTrim).FinalQuery);
        var expectedPath = $"/services/data/{apiVersion}/search/?q={finalQuery}";
        return m => m.Send(It.Is<HttpRequestMessage>(req => ValidRequest(req, skipTrim, expectedPath)));
    }

    /// <summary>
    /// Extracts soql query from request and trims if requested. This will no longer be necessary when all Salesforce queries use QueryExtensions.cs
    /// </summary>
    private static bool ValidRequest(HttpRequestMessage req, bool skipTrim, string expectedPath)
    {
        if (req.Method != HttpMethod.Get || req.RequestUri is null)
            return false;
        if (skipTrim)
            return req.RequestUri.PathAndQuery == expectedPath;
        if (MockableHttpMessageHandler.MatchSalesforcePath(req.RequestUri?.PathAndQuery) is { Success: true, Groups: var groups })
        {
            var soql = Uri.UnescapeDataString(groups["soql"].Value);
            var trimmed = soqlTrim.Replace(soql.Trim(), " ");
            var pathAndQuery = req.RequestUri!.AbsolutePath + $"?q={Uri.EscapeDataString(trimmed)}";
            return pathAndQuery == expectedPath;
        }
        return false;
    }

    public static Moq.Language.Flow.IReturnsResult<MockableHttpMessageHandler> ReturnsSalesforceQueryResult<T>(this Moq.Language.IReturns<MockableHttpMessageHandler, HttpResponseMessage> target, params T[] records)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        return target.ReturnsJsonResponse(new QueryResponse<T>
        {
            Records = records.ToList(),
            Done = true,
            TotalSize = records.Length,
        });
    }

    public static Moq.Language.Flow.IReturnsResult<MockableHttpMessageHandler> ReturnsSalesforceSearchResult<T>(this Moq.Language.IReturns<MockableHttpMessageHandler, HttpResponseMessage> target, params T[] records)
    {
        return target.ReturnsJsonResponse(new SearchResponse<T>
        {
            Records = records.ToList()
        });
    }
}