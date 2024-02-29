using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrincipleStudios.Salesforce;

public static class ApiExtensionMethods
{
    public static Moq.Language.Flow.IReturnsResult<MockableHttpMessageHandler> ReturnsJsonResponse<T>(
        this Moq.Language.IReturns<MockableHttpMessageHandler, HttpResponseMessage> target,
        T response,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        return target.Returns(new HttpResponseMessage
        {
            StatusCode = httpStatusCode,
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(response), Encoding.UTF8, "application/json"),
        });
    }
}
