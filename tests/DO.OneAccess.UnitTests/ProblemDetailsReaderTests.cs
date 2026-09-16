using System.Net;
using System.Text;
using DO.OneAccess.Client.Services;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class ProblemDetailsReaderTests
{
    [Fact]
    public async Task ReadFromResponseAsync_StandardProblemDetails_ParsesTitleAndDetail()
    {
        var json = """
        {
            "title": "Resource Not Found",
            "status": 404,
            "detail": "The requested system with ID 123 was not found."
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/problem+json")
        };

        var exception = await ProblemDetailsReader.ReadFromResponseAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("Resource Not Found", exception.Title);
        Assert.Equal("The requested system with ID 123 was not found.", exception.Detail);
        Assert.Null(exception.Errors);
        Assert.Equal("The requested system with ID 123 was not found.", exception.GetFirstErrorMessage());
    }

    [Fact]
    public async Task ReadFromResponseAsync_ValidationProblemDetails_ParsesErrorsDictionary()
    {
        var json = """
        {
            "title": "One or more validation errors occurred.",
            "status": 400,
            "errors": {
                "Username": ["Username is required.", "Username must be at least 3 characters."],
                "Password": ["Password is too short."]
            }
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/problem+json")
        };

        var exception = await ProblemDetailsReader.ReadFromResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("One or more validation errors occurred.", exception.Title);
        Assert.NotNull(exception.Errors);
        Assert.Equal(2, exception.Errors.Count);
        Assert.True(exception.Errors.ContainsKey("Username"));
        Assert.Equal(2, exception.Errors["Username"].Length);
        Assert.Equal("Username is required.", exception.GetFirstErrorMessage());
    }

    [Fact]
    public async Task ReadFromResponseAsync_NonJsonOrHtmlError_ReturnsGracefulDefault()
    {
        var html = "<html><body>502 Bad Gateway</body></html>";

        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html"),
            ReasonPhrase = "Bad Gateway"
        };

        var exception = await ProblemDetailsReader.ReadFromResponseAsync(response);

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("Bad Gateway", exception.Title);
        Assert.Null(exception.Detail);
        Assert.Null(exception.Errors);
        Assert.Equal("Bad Gateway", exception.GetFirstErrorMessage());
    }

    [Fact]
    public async Task ReadFromResponseAsync_EmptyContent_ReturnsDefaultException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(string.Empty),
            ReasonPhrase = "Internal Server Error"
        };

        var exception = await ProblemDetailsReader.ReadFromResponseAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal("Internal Server Error", exception.Title);
    }
}
