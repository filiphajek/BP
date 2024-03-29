﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using TaskLauncher.App.DAL;
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Controllers.Base;

/// <summary>
/// Abstraktni bazova trida pro uzivatelske kontrolery s odata
/// </summary>
[ODataRouteComponent("odata/user")]
[Route("api/[controller]")]
[Authorize(Policy = Constants.Policies.UserPolicy)]
public abstract class UserODataController<TResponse> : ControllerBase
where TResponse : class
{
    protected readonly AppDbContext context;

    public UserODataController(AppDbContext context)
    {
        this.context = context;
    }
}