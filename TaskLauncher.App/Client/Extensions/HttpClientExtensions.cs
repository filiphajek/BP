using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using System.Text;

namespace TaskLauncher.App.Client.Extensions;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PatchAsJsonAsync<TModel>(this HttpClient client, string uri, JsonPatchDocument<TModel> patchDoc) 
        where TModel : class
    {
        var requestContent = new StringContent(JsonConvert.SerializeObject(patchDoc), Encoding.UTF8, "application/json-patch+json");
        return await client.PatchAsync(uri, requestContent);
    }
}
