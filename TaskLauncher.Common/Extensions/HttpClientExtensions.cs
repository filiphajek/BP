using System.Net.Http.Headers;

namespace TaskLauncher.Common.Extensions;

public static class HttpClientExtensions
{
    /// <summary>
    /// Posilani HTTP dotaz, ktery obsahuje soubor a payload, posila se jako multipart/form-data
    /// </summary>
    public static async Task<HttpResponseMessage> SendMultipartFormDataAsync<T>(this HttpClient httpClient, string uri, Stream file, T payload, string? fileName = null)
    {
        using var request = new HttpRequestMessage();

        var boundary = Guid.NewGuid().ToString();
        var content = new MultipartFormDataContent(boundary);
        content.Headers.Remove("Content-Type");
        content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

        foreach (var (Content, Name) in GetStringContentsFrom(payload))
        {
            content.Add(Content, Name);
        }

        var contentFile = new StreamContent(file);
        contentFile.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(contentFile, "file", fileName ?? "file");
        request.Content = content;
        request.Method = new HttpMethod("POST");
        request.RequestUri = new Uri(uri, UriKind.RelativeOrAbsolute);
        return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    }

    private static IEnumerable<(StringContent Content, string Name)> GetStringContentsFrom<T>(T obj)
    {
        var properties = obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.PropertyType != typeof(string))
                continue;

            var value = property.GetValue(obj);
            if (value is not null)
                yield return new(new StringContent(value.ToString()), property.Name);
        }
    }
}
