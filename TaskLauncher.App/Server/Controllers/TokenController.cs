using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Repositories;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers;

public class TokenController : BaseController
{
    private readonly AppDbContext context;
    private readonly ITokenBalanceRepository tokenRepository;
    private readonly IMapper mapper;

    public TokenController(AppDbContext context, ITokenBalanceRepository tokenRepository, IMapper mapper, ILogger<TokenController> logger) : base(logger)
    {
        this.context = context;
        this.tokenRepository = tokenRepository;
        this.mapper = mapper;
    }

    [Authorize(Policy = "admin-policy")]
    [HttpGet("{userId}")]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync([FromRoute] string userId)
    {
        var tokenBalance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == userId);
        if (tokenBalance is null)
            return BadRequest();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    [Authorize(Policy = "user-policy")]
    [HttpGet]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync()
    {
        var tokenBalance = await context.TokenBalances.SingleAsync();
        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    [Authorize(Policy = "admin-policy")]
    [HttpPut]
    public async Task<ActionResult> UpdateTokenBalanceAsync([FromBody] UpdateBalanceRequest request)
    {
        await semaphoreSlim.WaitAsync();
        var balance = await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == request.UserId);
        if (balance is null)
            return BadRequest();

        balance.CurrentAmount = request.Amount;
        await tokenRepository.UpdateAsync(balance);
        semaphoreSlim.Release();
        return Ok();
    }
}
