using Moq;
using Salesforce.Common.Models.Json;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
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

        return target.Setup(query.ToSalesforceQueryCallExpression(apiVersion, skipTrim, true));
    }

    public static Expression<Func<MockableHttpMessageHandler, HttpResponseMessage>> ToSalesforceQueryCallExpression(this FormattableString query, string apiVersion, bool skipTrim = false, bool isSearch = false)
    {
        var finalQuery = Uri.EscapeDataString(query.ToSoqlQuery(skipTrim).FinalQuery);
        var expectedPath = isSearch
            ? $"/services/data/{apiVersion}/search/?q={finalQuery}"
            : $"/services/data/{apiVersion}/query?q={finalQuery}";
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

    public static Moq.Language.Flow.IReturnsResult<MockableHttpMessageHandler> ReturnsSalesforceResult(this Moq.Language.IReturns<MockableHttpMessageHandler, HttpResponseMessage> target, params object[] records)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        return target.ReturnsJsonResponse(new QueryResult<object>
        {
            Records = records.ToList(),
            Done = true,
            TotalSize = records.Length,
        });
    }

    public static Moq.Language.Flow.IReturnsResult<MockableHttpMessageHandler> ReturnsSalesforceSearchResult(this Moq.Language.IReturns<MockableHttpMessageHandler, HttpResponseMessage> target, params object[] records)
    {
        return target.ReturnsJsonResponse(new
        {
            SearchRecords = records.ToList()
        });
    }
}