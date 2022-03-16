using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;

namespace TaskLauncher.Api.Controllers;

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

    [Authorize(Policy = "user-policy")]
    [HttpGet]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync(string? userId = null)
    {
        TokenBalanceEntity? tokenBalance = null;
        if(userId is null)
            tokenBalance = (await tokenRepository.GetAllAsync()).SingleOrDefault();
        else
            tokenBalance = (await context.TokenBalances.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == userId));
        
        if (tokenBalance is null)
            return BadRequest();

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
