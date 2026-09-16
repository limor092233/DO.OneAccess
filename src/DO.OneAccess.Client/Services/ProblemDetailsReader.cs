using System.Net;
using System.Text.Json;
using DO.OneAccess.Client.Auth;

namespace DO.OneAccess.Client.Services;

public static class ProblemDetailsReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ProblemDetailsException> ReadFromResponseAsync(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        var defaultTitle = response.ReasonPhrase ?? $"HTTP {(int)statusCode}";

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ProblemDetailsException(statusCode, defaultTitle);
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string title = defaultTitle;
            if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                title = titleProp.GetString() ?? defaultTitle;
            }

            string? detail = null;
            if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
            {
                detail = detailProp.GetString();
            }

            Dictionary<string, string[]>? errors = null;
            if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
            {
                errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var messages = prop.Value.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .ToArray();
                        errors[prop.Name] = messages;
                    }
                }
            }

            return new ProblemDetailsException(statusCode, title, detail, errors);
        }
        catch
        {
            return new ProblemDetailsException(statusCode, defaultTitle);
        }
    }
}
