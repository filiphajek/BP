﻿using Microsoft.Extensions.Options;
using TaskLauncher.WebApp.Server.Auth0;

namespace TaskLauncher.WebApp.Server.Proxy;

public class Auth0ManagementTokenMiddleware : ProxyHandlerMiddleware
{
    private readonly ManagementTokenService managementTokenService;

    public Auth0ManagementTokenMiddleware(IOptions<ReverseProxyHandlers> options, ManagementTokenService managementTokenService) : base(options)
    {
        this.managementTokenService = managementTokenService;
    }

    public override async Task HandleAsync(HttpContext context)
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        context.Request.Headers.Authorization = $"Bearer {accessToken}";
    }
}