using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Kontroler ktery meni a zobrazuje hodnotu tokenu
/// </summary>
public class TokenController : BaseController
{
    private readonly AppDbContext context;
    private readonly IMapper mapper;
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    public TokenController(AppDbContext context, IMapper mapper, ILogger<TokenController> logger) : base(logger)
    {
        this.context = context;
        this.mapper = mapper;
    }

    /// <summary>
    /// Vrací zůstatek tokenů daného uživatele
    /// </summary>
    /// <param name="userId" example="auth0|622033411a44b70076f2790">Id uživatele</param>
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.AdminPolicy)]
    [HttpGet("/api/admin/token")]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync(string userId)
    {
        var tokenBalance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == userId);
        if (tokenBalance is null)
            return NotFound();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    /// <summary>
    /// Vrací zůstatek tokenů přihlášeného uživatele
    /// </summary>
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.UserPolicy)]
    [HttpGet]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync()
    {
        var tokenBalance = await context.TokenBalances.SingleAsync();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    /// <summary>
    /// Aktualizace zůstatku tokenů daného uživatele
    /// </summary>
    [Consumes("application/json")]
    [Authorize(Policy = Constants.Policies.AdminPolicy)]
    [HttpPut("/api/admin/token")]
    public async Task<ActionResult> UpdateTokenBalanceAsync([FromBody] UpdateBalanceRequest request)
    {
        await semaphoreSlim.WaitAsync();
        var balance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == request.UserId);
        if (balance is null)
            return NotFound();

        balance.CurrentAmount = request.Amount;
        context.Update(balance);
        await context.SaveChangesAsync();
        semaphoreSlim.Release();
        return Ok();
    }
}
