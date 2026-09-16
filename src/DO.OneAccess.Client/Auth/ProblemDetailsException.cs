using System.Net;

namespace DO.OneAccess.Client.Auth;

public class ProblemDetailsException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public ProblemDetailsException(
        HttpStatusCode statusCode,
        string title,
        string? detail = null,
        IDictionary<string, string[]>? errors = null)
        : base(detail ?? title)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Errors = errors;
    }

    public string GetFirstErrorMessage()
    {
        if (Errors != null && Errors.Count > 0)
        {
            var firstEntry = Errors.Values.FirstOrDefault();
            if (firstEntry != null && firstEntry.Length > 0)
            {
                return firstEntry[0];
            }
        }

        return Detail ?? Title;
    }
}
