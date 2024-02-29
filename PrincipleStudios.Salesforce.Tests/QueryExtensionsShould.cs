using Moq;
using Salesforce.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace PrincipleStudios.Salesforce;

public class QueryExtensionsShould
{
    [Fact]
    public void HandleStringParameters()
    {
        // Arrange
        var username = "test@example.com";

        // Act/Assert
        TestHandlingParameters($"SELECT Id FROM User WHERE Username={username}",
            "SELECT Id FROM User WHERE Username='test@example.com'",
            "SELECT Id FROM User WHERE Username={0}");
    }

    [MemberData(nameof(ParameterReplacement))]
    [InlineData(null, "null")] // null does not get escaped
    [InlineData(3, "3")]
    [InlineData((sbyte)3, "3")]
    [InlineData((short)3, "3")]
    [InlineData((long)3, "3")]
    [InlineData((byte)3, "3")]
    [InlineData((ushort)3, "3")]
    [InlineData((uint)3, "3")]
    [InlineData((ulong)3, "3")]
    [InlineData((float)3.05f, "3.05")]
    [InlineData((double)3.05, "3.05")]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    [InlineData("test@example.com", "'test@example.com'")]
    [InlineData("test'foo@example\\.com", "'test\\'foo@example\\\\.com'")]
    [Theory]
    public void HandleParameterReplacementOfManyTypes(object? parameter, string expected)
    {
        TestHandlingParameters($"{parameter}", expected, "{0}");
    }

    public static IEnumerable<object[]> ParameterReplacement()
    {
        // Decimals are apparently not constant expressions
        yield return new object[] { (decimal)3.05, "3.05" };

        // Dates do not get quoted
        yield return new object[] { new DateOnly(2000, 1, 1), "2000-01-01" };
        yield return new object[] { new DateTime(2000, 1, 1), "2000-01-01" };
        yield return new object[] { new DateTimeOffset(2000, 1, 1, 12, 05, 17, TimeSpan.Zero), "2000-01-01T12:05:17.0000000+00:00" };

        // Arrays are put in parentheses for IN clauses
        yield return new object[] { new[] { "test@example.com", "test'foo@example\\.com" }, "('test@example.com','test\\'foo@example\\\\.com')" };
        yield return new object[] { new[] { new DateOnly(2000, 1, 1), new DateOnly(1990, 1, 1) }, "(2000-01-01,1990-01-01)" };

        // Allow pre-escaped data
        yield return new object[] { new EscapedSoslQuery("foo*bar"), "{foo*bar}" };

        // Allow pre-escaped query
        yield return new object[] { new EscapedQuery("select id from foo where bar = '1'"), "select id from foo where bar = '1'" };
        yield return new object[] { new EscapedQuery("select id from foo where bar = '    '"), "select id from foo where bar = '    '" };

        // Allow nested formattable strings
        FormattableString query = $"WHERE Field1 = {1} AND Field2 = 'test'";
        yield return new object[]
        {
            query, "WHERE Field1 = 1 AND Field2 = 'test'"
        };

        // Allow recursively nested formattable strings
        FormattableString clause = $"Id = {"some-user-id"}";
        FormattableString fullSoqlQuery = $"SELECT Id FROM User WHERE {clause}";
        yield return new object[]
        {
            fullSoqlQuery,
            "SELECT Id FROM User WHERE Id = 'some-user-id'"
        };
    }

    [Fact]
    public void HandleTrimmingQueries()
    {
        // Arrange
        var username = "test@example.com";

        // Act/Assert
        TestHandlingParameters(@$"
            SELECT Id
            FROM User
            WHERE Username={username}
        ",
            "SELECT Id FROM User WHERE Username='test@example.com'", 
            "SELECT Id FROM User WHERE Username={0}");
    }

    [Fact]
    public void HandleStandardizingQueryLineEndings()
    {
        // Arrange
        var username = "test@example.com";

        // Act/Assert
        TestHandlingParameters($"SELECT Id\r\nFROM User\rWHERE\nUsername={username}",
            "SELECT Id FROM User WHERE Username='test@example.com'",
            "SELECT Id FROM User WHERE Username={0}");
    }

    [Fact]
    public void ThrowForUnhandledTypes()
    {
        Assert.Throws<UnknownSalesforceParameterException>(() => QueryExtensions.ToSoqlQuery($"{new object()}"));
    }

    private static void TestHandlingParameters(FormattableString original, string expectedFinalQuery, string expectedTrimmed)
    {
        // Act
        var actual = QueryExtensions.ToSoqlQuery(original);

        // Assert
        Assert.Equal(expectedFinalQuery, actual.FinalQuery);
        Assert.Equal(expectedTrimmed, actual.Format);
    }

    [Fact]
    public void AllowSkippingTrimming()
    {
        // Arrange
        var username = "test@example.com";
        FormattableString query = @$"
            SELECT Id
            FROM User
            WHERE Username={username}
        ";

        // Act
        var actual = QueryExtensions.ToSoqlQuery(query, skipTrim: true);

        // Assert
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, query.Format, "'test@example.com'"), actual.FinalQuery);
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, query.Format, "{0}"), actual.Format);
    }

    [Fact]
    public void EscapeSoslExpressions()
    {
        // Act
        var actual = "\\?&|!{}[]()^~*:\"'+-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 \t".EscapeSoslQuery();

        // Assert
        Assert.Equal("\\\\\\?\\&\\|\\!\\{\\}\\[\\]\\(\\)\\^\\~\\*\\:\\\"\\'\\+\\-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 \t", actual.Value);
    }

    [Fact]
    public void HandleErroneouslyNullSoslExpressions()
    {
        // Act
        var actual = ((string)null!).EscapeSoslQuery();

        // Assert
        Assert.Equal("", actual.Value);
    }

    [Fact]
    public async Task MakeItEasyToIssueASoqlQuery()
    {
        const string id = "foo";
        const string apiVersion = "v54.0";

        // Arrange
        var (mock, client) = Setup();
        FormattableString query = $"SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, apiVersion).ReturnsSalesforceQueryResult(new { Id = id }).Verifiable();

        // Act
        var result = await client.QueryAsync(query, apiVersion).ConfigureAwait(false);

        // Assert
        Assert.Collection(result.Records, record =>
        {
            Assert.NotNull(record["Id"]);
            Assert.Equal(id, record.Value<string>("Id"));
        });
        mock.Verify(query.ToSalesforceQueryCallExpression(apiVersion), Times.Once());
    }

    [Fact]
    public async Task MakeItEasyToQuerySoqlValues()
    {
        const string id = "foo";
        const string apiVersion = "v54.0";

        // Arrange
        var (mock, client) = Setup();
        FormattableString query = $"SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, apiVersion).ReturnsSalesforceQueryResult(new { Id = id }).Verifiable();

        // Act
        var result = await client.QueryAsync(query, apiVersion).ConfigureAwait(false);

        // Assert
        Assert.Collection(result.Records, record =>
        {
            Assert.Equal(id, record.SelectToken("Id")?.ToObject<string>());
        });
        mock.Verify(query.ToSalesforceQueryCallExpression(apiVersion), Times.Once());
    }

    [Fact]
    public async Task WrapAMalformedQueryException()
    {
        const string apiVersion = "v54.0";
       
        // Arrange
        var (mock, client) = Setup();
        FormattableString query = $"bad SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, apiVersion).Throws(new ForceException(global::Salesforce.Common.Models.Json.Error.MalformedQuery, "unexpected token: bad")).Verifiable();

        // Act & assert
        await Assert.ThrowsAsync<ForceException>(async () => await client.QueryAsync(query, apiVersion).ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static (Mock<MockableHttpMessageHandler> mock, JsonHttpClient client) Setup()
    {
        var mock = new Mock<MockableHttpMessageHandler>(MockBehavior.Loose) { CallBase = true };
        var httpClient = new HttpClient(mock.Object)
        {
            BaseAddress = new Uri("https://example.com"),
        };
        var client = new JsonHttpClient("https://example.com", "v54.0", null, httpClient, true);

        return (mock, client);
    }
}
