using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories;

namespace TaskLauncher.Api.Controllers;

[Authorize]
public class TokenController : BaseController
{
    private readonly ITokenBalanceRepository tokenRepository;
    private readonly IMapper mapper;

    public TokenController(ITokenBalanceRepository tokenRepository, IMapper mapper, ILogger<TokenController> logger) : base(logger)
    {
        this.tokenRepository = tokenRepository;
        this.mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<TokenBalanceResponse>> GetTokenBalanceAsync(string? userId = null)
    {
        TokenBalanceEntity? tokenBalance = null;
        if(userId is null)
            tokenBalance = (await tokenRepository.GetAllAsync()).SingleOrDefault();
        else
            tokenBalance = (await tokenRepository.FindAsync(i => i.UserId == userId)).FirstOrDefault();
        
        if (tokenBalance is null)
            return BadRequest();

        return Ok(mapper.Map<TokenBalanceResponse>(tokenBalance));
    }

    [Authorize(Policy = "updateToken")]
    [HttpPut]
    public async Task<ActionResult> UpdateTokenBalanceAsync(double amount, string userId)
    {
        var balance = (await tokenRepository.FindAsync(i => i.UserId == userId)).FirstOrDefault();
        if (balance is null)
            return BadRequest();

        balance.CurrentAmount += amount;
        await tokenRepository.UpdateAsync(balance); 
        return Ok();
    }
}
