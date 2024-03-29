using System.Globalization;
using System.Text.RegularExpressions;

namespace PrincipleStudios.Salesforce;

public static class QueryExtensions
{
    private static string SoqlQuotedString(string s)
    {
        return $"'{SoqlQuotedStringEscape(s)}'";
    }
    private static string? SoqlQuotedStringEscape(string s)
    {
        return s
#if NETSTANDARD2_0
            .Replace(@"\", @"\\")
            .Replace("'", @"\'");
#else
            .Replace(@"\", @"\\", StringComparison.InvariantCulture)
            .Replace("'", @"\'", StringComparison.InvariantCulture);
#endif
    }


    //sosl reserved characters: ? & | ! { } [ ] ( ) ^ ~ * : \ " ' + - 
    private static readonly Regex SoslReservedCharacters = new Regex("[\\\\?&|!{}[\\]\\(\\)^~*:\"'+-]", RegexOptions.Compiled);
    public static EscapedSoslQuery EscapeSoslQuery(this string query)
    {
        if (query is null) return EscapedSoslQuery.Empty;
        return new EscapedSoslQuery(SoslReservedCharacters.Replace(query, match => "\\" + match.Value));
    }
    private static string EscapeSoqlObject(object? input, bool skipTrim)
    {
        return input switch
        {
            // no quotes around numbers, dates, and null
            null => "null",
            sbyte number => number.ToString(CultureInfo.InvariantCulture),
            short number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            byte number => number.ToString(CultureInfo.InvariantCulture),
            ushort number => number.ToString(CultureInfo.InvariantCulture),
            uint number => number.ToString(CultureInfo.InvariantCulture),
            ulong number => number.ToString(CultureInfo.InvariantCulture),
            float number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
#endif
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToString("O"),
            // quotes and escape for other args
            string s => SoqlQuotedString(s),
            // Sosl is already escaped
            EscapedSoslQuery { Value: var s } => $"{{{s}}}",
            // Query is already escaped
            EscapedQuery { Value: var s } => s,
            // Handle lists
            IEnumerable<object> list => $"({string.Join(",", list.Select(i => EscapeSoqlObject(i, skipTrim)))})",
            System.Collections.IEnumerable list => EscapeSoqlObject(list.Cast<object>(), skipTrim),
            FormattableString fs => string.Format(
                CultureInfo.InvariantCulture,
                skipTrim
                    ? fs.Format
                    : soqlTrim.Replace(fs.Format.Trim(), " "),
                fs.GetArguments().Select(arg => EscapeSoqlObject(arg, skipTrim)).ToArray()),
            // force proper type handling
            _ => throw new UnknownSalesforceParameterException(),
        };
    }

    private static readonly Regex soqlTrim = new Regex(@"\s+", RegexOptions.Compiled);

    public static SalesforceQuery ToSoqlQuery(this FormattableString query, bool skipTrim = false)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var args = query.GetArguments()
            .Select(arg => EscapeSoqlObject(arg, skipTrim))
            .ToArray();
        var trimmed = skipTrim
                ? query.Format
                : soqlTrim.Replace(query.Format.Trim(), " ");
        var finalQuery = string.Format(
            CultureInfo.InvariantCulture,
            trimmed,
            args
        );
        return new(trimmed, finalQuery);
    }

    public static SalesforceQuery ToSoslQuery(this FormattableString query, bool skipTrim = false) =>
        // It is my understanding that SOQL and SOSL are escaped the same way. Providing two separate methods just in case taht is not the case.
        ToSoqlQuery(query, skipTrim);
}

public record struct EscapedSoslQuery(string Value)
{
    public static readonly EscapedSoslQuery Empty = new EscapedSoslQuery(string.Empty);
}

public record struct EscapedQuery(string Value)
{
    public static readonly EscapedQuery Empty = new EscapedQuery(string.Empty);
}

public record struct SalesforceQuery(string Format, string FinalQuery);
