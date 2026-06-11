using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartEventPlatformWeb.Infrastructure;

public static class ApiHttpHelper
{
    public static async Task<List<T>> GetListAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
    }

    public static async Task<T?> GetNullableAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public static async Task<ApiOperationResult<long>> PostAndReadIdAsync<T>(HttpClient client, string url, T dto)
    {
        var response = await client.PostAsJsonAsync(url, dto);

        if (response.IsSuccessStatusCode)
        {
            var id = await response.Content.ReadFromJsonAsync<long>();
            return ApiOperationResult<long>.Ok(response.StatusCode, id);
        }

        if (IsExpectedApiError(response.StatusCode))
        {
            var message = await ReadApiErrorMessageAsync(response);
            return ApiOperationResult<long>.Fail(response.StatusCode, message);
        }

        throw await CreateApiExceptionAsync(response);
    }

    public static async Task<ApiOperationResult> PostAsync<T>(HttpClient client, string url, T dto)
    {
        var response = await client.PostAsJsonAsync(url, dto);

        if (response.IsSuccessStatusCode)
        {
            return ApiOperationResult.Ok(response.StatusCode);
        }

        if (IsExpectedApiError(response.StatusCode))
        {
            var message = await ReadApiErrorMessageAsync(response);
            return ApiOperationResult.Fail(response.StatusCode, message);
        }

        throw await CreateApiExceptionAsync(response);
    }

    public static async Task<ApiOperationResult> PutAsync<T>(HttpClient client, string url, T dto)
    {
        var response = await client.PutAsJsonAsync(url, dto);

        if (response.IsSuccessStatusCode)
        {
            return ApiOperationResult.Ok(response.StatusCode);
        }

        if (IsExpectedApiError(response.StatusCode))
        {
            var message = await ReadApiErrorMessageAsync(response);
            return ApiOperationResult.Fail(response.StatusCode, message);
        }

        throw await CreateApiExceptionAsync(response);
    }

    public static async Task<ApiOperationResult> DeleteAsync(HttpClient client, string url)
    {
        var response = await client.DeleteAsync(url);

        if (response.IsSuccessStatusCode)
        {
            return ApiOperationResult.Ok(response.StatusCode);
        }

        if (IsExpectedApiError(response.StatusCode))
        {
            var message = await ReadApiErrorMessageAsync(response);
            return ApiOperationResult.Fail(response.StatusCode, message);
        }

        throw await CreateApiExceptionAsync(response);
    }

    private static bool IsExpectedApiError(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.BadRequest ||
               statusCode == HttpStatusCode.NotFound ||
               statusCode == HttpStatusCode.Conflict ||
               statusCode == HttpStatusCode.UnprocessableEntity ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    private static async Task<string> ReadApiErrorMessageAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();//cita ceo sadrzaj odgovora kao string

        if (string.IsNullOrWhiteSpace(content))
        {
            return "The operation could not be completed.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind == JsonValueKind.String)//ako je obican string, npr. "Event not found"
            {
                var text = document.RootElement.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            if (document.RootElement.TryGetProperty("detail", out var detailElement))
            {
                var detail = detailElement.GetString();

                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail;
                }
            }

            if (document.RootElement.TryGetProperty("title", out var titleElement))
            {
                var title = titleElement.GetString();

                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            if (document.RootElement.TryGetProperty("errors", out var errorsElement))
            {
                var messages = new List<string>();

                foreach (var property in errorsElement.EnumerateObject())
                {
                    foreach (var error in property.Value.EnumerateArray())
                    {
                        var message = error.GetString();

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            messages.Add(message);
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }
        }
        catch (JsonException)
        {
            return content.Trim('"');//skida navodnike sa pocetka i kraja ako je content obican string koji nije validan JSON, npr. "\"Event not found\""
        }

        return content.Trim('"');
    }

    private static async Task<Exception> CreateApiExceptionAsync(HttpResponseMessage response)
    {
        var message = await ReadApiErrorMessageAsync(response);

        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"API request failed with status code {(int)response.StatusCode}.";
        }

        return new HttpRequestException(message, null, response.StatusCode);
    }
}