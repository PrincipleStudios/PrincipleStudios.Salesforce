using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrincipleStudios.Salesforce;

internal static partial class LoggingExtensions
{
    [LoggerMessage(LogLevel.Debug, "Executing SOQL query: {Query}, parameterized from: {QueryWithPlaceholders}")]
    public static partial void LogSoqlQuery(this ILogger logger, string query, string queryWithPlaceholders);

    [LoggerMessage(LogLevel.Debug, "Requesting next page for SOQL query: {PageUrl}")]
    public static partial void LogSoqlNextPage(this ILogger logger, Uri PageUrl);

    [LoggerMessage(LogLevel.Debug, "Executing SOSL search: {Query}, parameterized from: {QueryWithPlaceholders}")]
    public static partial void LogSoslSearch(this ILogger logger, string query, string queryWithPlaceholders);
}
