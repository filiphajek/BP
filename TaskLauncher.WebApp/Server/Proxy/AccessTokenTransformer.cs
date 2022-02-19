using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Forwarder;

namespace TaskLauncher.WebApp.Server.Proxy;

public class AccessTokenTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

        var cache = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImI1Wk1YcmFOOE82YUlxTUJtZnhDViJ9.eyJpc3MiOiJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vIiwic3ViIjoiYXV0aDB8NjFiMGUxNjE2NzhhMGMwMDY4OTY0NGUwIiwiYXVkIjpbImh0dHBzOi8vd3V0c2hvdC10ZXN0LWFwaS5jb20iLCJodHRwczovL2Rldi04bmh1eGF5MS51cy5hdXRoMC5jb20vdXNlcmluZm8iXSwiaWF0IjoxNjQ1MTEzMjgzLCJleHAiOjE2NDUxOTk2ODMsImF6cCI6Ijd3bjBsRG5COWhWNjJtODZ6aDhYYjM3NEtoSHhPaXJKIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCJ9.j0zD-yhn7Ol1WZRhzZuCOeMFfZswTUNX60S6iZBefurJfgtfz5ux_LEN2bdiictsESlu-EwXl-JykjLa3CZgA_m5Z3wYQxuTBYqB5i8cfyvE2eYM77iLLZ8jgImknAaw2tuB30Rb5iX2vEDbgKP4vnhUZtwb6zFvVOHFMccJFGSnogVuOb31fydUtc0VCCca3NmrYOiKDZSGWiDz-nvLUMpPC39yRNdyCDSYoxrkIwgHSph8DmughJTHohHInexpixtUr1BfBp2RK-dYFD4STm1VuBk8mlH3NaQVAqFBMOxylUJ2ByHKGSze0gXZBSluZS-Z57a5kspEInVRL7jkkw";

        if (string.IsNullOrEmpty(cache))
            cache = await httpContext.GetUserAccessTokenAsync();

        httpContext.Request.Headers.Add("Authorization", $"Bearer {cache}");
    }
}
