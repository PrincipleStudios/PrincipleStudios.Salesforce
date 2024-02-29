using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrincipleStudios.Salesforce;

public interface IGetToken
{
    ValueTask<string> GetAccessTokenAsync();
    ValueTask<string?> GetNewAccessTokenAsync();
}

public class SalesforceAuthenticationHttpHandlerShould
{
    private readonly Mock<MockableHttpMessageHandler> mockBaseHandler;
    private readonly Mock<IGetToken> mockImplementation;
    private readonly ConcreteHandler target;
    private readonly HttpClient client;

    private sealed class ConcreteHandler : SalesforceAuthenticationHttpHandler
    {
        private readonly IGetToken tokenAccessor;

        public ConcreteHandler(IGetToken tokenAccessor) : base()
        {
            this.tokenAccessor = tokenAccessor;
        }
        public ConcreteHandler(IGetToken tokenAccessor, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            this.tokenAccessor = tokenAccessor;
        }

        protected override ValueTask<string> GetAccessTokenAsync() => tokenAccessor.GetAccessTokenAsync();
        protected override ValueTask<string?> GetNewAccessTokenAsync() => tokenAccessor.GetNewAccessTokenAsync();
    }

    public SalesforceAuthenticationHttpHandlerShould()
    {
        mockBaseHandler = new Mock<MockableHttpMessageHandler>(MockBehavior.Loose) { CallBase = true };
        mockImplementation = new Mock<IGetToken>();
        target = new ConcreteHandler(mockImplementation.Object, mockBaseHandler.Object);
        client = new HttpClient(target)
        {
            BaseAddress = new("https://example.com"),
        };
    }

    [Fact]
    public async Task Allows_the_default_constructor()
    {
        // Arrange
        var expectedToken = "GOOD_TOKEN";
        var expectedResponse = new HttpResponseMessage();
        var request = new HttpRequestMessage();

        var target = new ConcreteHandler(mockImplementation.Object) { InnerHandler = mockBaseHandler.Object };
        var client = new HttpClient(target)
        {
            BaseAddress = new("https://example.com"),
        };
        mockBaseHandler.Setup(h => h.Send(It.Is<HttpRequestMessage>((rq) => MatchingRequest(rq, request, expectedToken)))).Returns(expectedResponse);
        mockImplementation.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(expectedToken);

        // Act
        var actualResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task Use_the_initial_access_token()
    {
        // Arrange
        var expectedToken = "GOOD_TOKEN";
        var expectedResponse = new HttpResponseMessage();
        var request = new HttpRequestMessage();
        mockBaseHandler.Setup(h => h.Send(It.Is<HttpRequestMessage>((rq) => MatchingRequest(rq, request, expectedToken)))).Returns(expectedResponse);
        mockImplementation.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(expectedToken);

        // Act
        var actualResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public async Task Returns_the_original_response_if_no_new_access_token_is_provided()
    {
        // Arrange
        var expectedToken = "BAD_TOKEN";
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        var request = new HttpRequestMessage();
        mockBaseHandler.Setup(h => h.Send(It.Is<HttpRequestMessage>((rq) => MatchingRequest(rq, request, expectedToken)))).Returns(expectedResponse);
        mockImplementation.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(expectedToken);
        mockImplementation.Setup(s => s.GetNewAccessTokenAsync()).ReturnsAsync((string?)null).Verifiable();

        // Act
        var actualResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
        mockImplementation.Verify(s => s.GetNewAccessTokenAsync());
    }

    [Fact]
    public async Task Resends_request_with_new_access_token()
    {
        // Arrange
        var goodToken = "GOOD_TOKEN";
        var badToken = "BAD_TOKEN";
        var badResponse = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        var expectedResponse = new HttpResponseMessage();
        var request = new HttpRequestMessage();
        mockBaseHandler.Setup(h => h.Send(It.Is<HttpRequestMessage>((rq) => MatchingRequest(rq, request, badToken)))).Returns(badResponse);
        mockBaseHandler.Setup(h => h.Send(It.Is<HttpRequestMessage>((rq) => MatchingRequest(rq, request, goodToken)))).Returns(expectedResponse);
        mockImplementation.Setup(s => s.GetAccessTokenAsync()).ReturnsAsync(badToken);
        mockImplementation.Setup(s => s.GetNewAccessTokenAsync()).ReturnsAsync(goodToken).Verifiable();

        // Act
        var actualResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, actualResponse);
        mockImplementation.Verify(s => s.GetNewAccessTokenAsync());
    }

    private static bool MatchingRequest(HttpRequestMessage actualRequest, HttpRequestMessage expectedRequest, string expectedToken)
    {
        return actualRequest == expectedRequest
            && actualRequest.Headers.Authorization?.Scheme == "Bearer"
            && actualRequest.Headers.Authorization?.Parameter == expectedToken;
    }
}
