using Microsoft.Extensions.Logging;
using Moq;

namespace PrincipleStudios.Salesforce;

public class SalesforceClientShould
{
    sealed record SampleObject(string Id);

    const string defaultApiVersion = "v54.0";
    private readonly Mock<MockableHttpMessageHandler> mock;
    private readonly SalesforceClient client;

    public SalesforceClientShould()
    {
        (mock, client) = Setup();
    }

    [Fact]
    public async Task Issue_soql_queries()
    {
        // Arrange
        var expected = new[] { new SampleObject("foo") };
        FormattableString query = $"SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, defaultApiVersion).ReturnsSalesforceQueryResult(expected).Verifiable();

        // Act
        var result = await client.QueryAsync<SampleObject>(query);

        // Assert
        Assert.Equal(expected, result.Records);
        mock.Verify(query.ToSalesforceQueryCallExpression(defaultApiVersion), Times.Once());
    }

    [Fact]
    public async Task Issue_soql_queries_with_overridden_api_version()
    {
        const string overriddenApiVersion = "v55.0";

        // Arrange
        var expected = new[] { new SampleObject("foo") };
        FormattableString query = $"SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, overriddenApiVersion).ReturnsSalesforceQueryResult(expected).Verifiable();

        // Act
        var result = await client.QueryAsync<SampleObject>(query, new() { ApiVersion = overriddenApiVersion });

        // Assert
        Assert.Equal(expected, result.Records);
        mock.Verify(query.ToSalesforceQueryCallExpression(overriddenApiVersion), Times.Once());
    }

    [Fact]
    public async Task Loads_next_records_from_a_previous_query_response()
    {
        const string expectedPathAndQuery = "/next-records";

        // Arrange
        var previous = new QueryResponse<SampleObject>
        {
            Records = Enumerable.Empty<SampleObject>(),
            Done = false,
            NextRecordsUrl = new(expectedPathAndQuery, UriKind.Relative)
        };
        var expected = new[] { new SampleObject("foo") };
        mock.Setup(h => h.Send(It.Is<HttpRequestMessage>(msg => msg.RequestUri != null && msg.RequestUri.PathAndQuery == expectedPathAndQuery)))
            .ReturnsSalesforceQueryResult(expected);

        // Act
        var result = await client.NextAsync<SampleObject>(previous);

        // Assert
        Assert.Equal(expected, result.Records);
    }

    [Fact]
    public async Task Provide_standard_HttpRequestException_on_failures()
    {
        // Arrange
        var expectedStatusCode = System.Net.HttpStatusCode.Forbidden;
        FormattableString query = $"SELECT Id FROM User WHERE Username={"test@example.com"}";
        mock.SetupSalesforceQuery(query, defaultApiVersion).Returns(new HttpResponseMessage(expectedStatusCode)).Verifiable();

        // Act
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.QueryAsync<SampleObject>(query));

        // Assert
        Assert.Equal(ex.StatusCode, expectedStatusCode);
        mock.Verify(query.ToSalesforceQueryCallExpression(defaultApiVersion), Times.Once());
    }

    [Fact]
    public async Task Issue_sosl_searches()
    {
        // Arrange
        var expected = new[] { new SampleObject("foo") };
        FormattableString query = $"FIND {"example.com"} RETURNING User(Id)";
        mock.SetupSalesforceSearch(query, defaultApiVersion).ReturnsSalesforceSearchResult(expected).Verifiable();

        // Act
        var result = await client.SearchAsync<SampleObject>(query);

        // Assert
        Assert.Equal(expected, result.Records);
        mock.Verify(query.ToSalesforceSearchCallExpression(defaultApiVersion), Times.Once());
    }

    [Fact]
    public async Task Issue_sosl_searches_with_overridden_api_version()
    {
        const string overriddenApiVersion = "v55.0";

        // Arrange
        var expected = new[] { new SampleObject("foo") };
        FormattableString query = $"FIND {"example.com"} RETURNING User(Id)";
        mock.SetupSalesforceSearch(query, overriddenApiVersion).ReturnsSalesforceSearchResult(expected).Verifiable();

        // Act
        var result = await client.SearchAsync<SampleObject>(query, new() { ApiVersion = overriddenApiVersion });

        // Assert
        Assert.Equal(expected, result.Records);
        mock.Verify(query.ToSalesforceSearchCallExpression(overriddenApiVersion), Times.Once());
    }

    [Fact]
    public async Task Pass_through_a_request_message()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage();
        var request = new HttpRequestMessage();
        FormattableString query = $"FIND {"example.com"} RETURNING User(Id)";
        mock.Setup(h => h.Send(request)).Returns(expectedResponse).Verifiable();

        // Act
        var actualResponse = await client.SendAsync(request); ;

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
        mock.Verify(h => h.Send(request), Times.Once());
    }

    private static (Mock<MockableHttpMessageHandler> mock, SalesforceClient client) Setup()
    {
        var mock = new Mock<MockableHttpMessageHandler>(MockBehavior.Loose) { CallBase = true };
        var baseAddress = new Uri("https://example.com");
        var httpClient = new HttpClient(mock.Object)
        {
            BaseAddress = baseAddress,
        };
        var client = new SalesforceClient(httpClient, baseAddress, defaultApiVersion, Mock.Of<ILogger<SalesforceClient>>());

        return (mock, client);
    }
}
