﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.Base;

[ODataRouteComponent("odata/user")]
[Route("api/[controller]")]
[Authorize(Policy = "user-policy")]
public abstract class UserODataController<TResponse> : ControllerBase
where TResponse : class
{
    protected readonly AppDbContext context;

    public UserODataController(AppDbContext context)
    {
        this.context = context;
    }
}