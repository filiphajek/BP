using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

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
    /// Admin endpoint pro zobrazeni zustatku tokenu daneho uzivatele
    /// </summary>
    [Authorize(Policy = "admin-policy")]
    [HttpGet("/api/admin/token")]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync(string userId)
    {
        var tokenBalance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == userId);
        if (tokenBalance is null)
            return BadRequest();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    /// <summary>
    /// Pro uzivatele, vraci zustatek tokenu
    /// </summary>
    [Authorize(Policy = "user-policy")]
    [HttpGet]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync()
    {
        var tokenBalance = await context.TokenBalances.SingleAsync();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    /// <summary>
    /// Aktualizace tokenu, pouze pro admina
    /// </summary>
    [Authorize(Policy = "admin-policy")]
    [HttpPut("/api/admin/token")]
    public async Task<ActionResult> UpdateTokenBalanceAsync([FromBody] UpdateBalanceRequest request)
    {
        await semaphoreSlim.WaitAsync();
        var balance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == request.UserId);
        if (balance is null)
            return BadRequest();

        balance.CurrentAmount = request.Amount;
        context.Update(balance);
        await context.SaveChangesAsync();
        semaphoreSlim.Release();
        return Ok();
    }
}
