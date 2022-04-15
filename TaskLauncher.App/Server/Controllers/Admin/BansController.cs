using Auth0.ManagementApi;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Controllers.Admin;

/// <summary>
/// Ban kontroler ke kteremu ma pristup pouze admin
/// </summary>
public class BansController : AdminODataController<BanResponse>
{
    private readonly IMapper mapper;
    private readonly IClientFactory<ManagementApiClient> clientFactory;
    private readonly Cache<UserClaimsModel> cache;

    public BansController(AppDbContext context, IMapper mapper, IClientFactory<ManagementApiClient> clientFactory, Cache<UserClaimsModel> cache) : base(context)
    {
        this.mapper = mapper;
        this.clientFactory = clientFactory;
        this.cache = cache;
    }

    /// <summary>
    /// Vrací všechny bany v systému, dotazuje se přes odata
    /// </summary>
    [ProducesResponseType(typeof(List<BanResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    [EnableQuery]
    public ActionResult<IQueryable<BanResponse>> Get()
    {
        return Ok(context.Bans.IgnoreQueryFilters().ProjectToType<BanResponse>());
    }

    /// <summary>
    /// Vrací detail banu
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id banu</param>
    [ProducesResponseType(typeof(BanResponse), 200)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BanResponse>> GetDetail([FromRoute] Guid id)
    {
        var ban = await context.Bans.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();
        return Ok(mapper.Map<BanResponse>(ban));
    }

    /// <summary>
    /// Vytvoření nového banu
    /// </summary>
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BanResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<IActionResult> BanUserAsync([FromBody] BanUserRequest request)
    {
        var auth0client = await clientFactory.GetClient();

        var user = await auth0client.Users.GetAsync(request.UserId);
        if (user.Blocked.HasValue && user.Blocked.Value)
            return BadRequest(new ErrorMessageResponse("User is already blocked"));

        var ban = new BanEntity { Description = request.Reason, Email = user.Email, UserId = request.UserId, Started = DateTime.Now };
        await context.Bans.AddAsync(ban);

        await auth0client.Users.UpdateAsync(request.UserId, new()
        {
            Blocked = true
        });

        await context.SaveChangesAsync();
        await UpdateCache(request.UserId, true);
        return Ok(mapper.Map<BanResponse>(ban));
    }

    /// <summary>
    /// Zrušení banu
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id banu</param>
    [ProducesResponseType(typeof(BanResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> UnBanUserAsync(Guid id)
    {
        var auth0client = await clientFactory.GetClient();

        var ban = await context.Bans.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();

        if(ban.Ended is not null)
            return NotFound();

        var user = await auth0client.Users.GetAsync(ban.UserId);
        if (user.Blocked.HasValue && !user.Blocked.Value)
            return BadRequest(new ErrorMessageResponse("User is not blocked"));

        ban.Ended = DateTime.Now;
        context.Update(ban);
        await context.SaveChangesAsync();

        await auth0client.Users.UpdateAsync(user.UserId, new()
        {
            Blocked = false,
        });

        await UpdateCache(user.UserId, false);
        return Ok(mapper.Map<BanResponse>(ban));
    }

    /// <summary>
    /// Smazání banu ze systému (ban musí být nejdříve zrušené)
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id banu</param>
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    [Produces("application/json")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBanAsync([FromRoute] Guid id)
    {
        var ban = await context.Bans.SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();

        if (ban.Ended is not null)
            return BadRequest(new ErrorMessageResponse("Cant delete ban. Ban is not cancel"));

        context.Remove(ban);
        await context.SaveChangesAsync();
        return Ok();
    }

    private async Task UpdateCache(string userId, bool blocked)
    {
        var cached = await cache.GetAsync(userId);
        if (cached is not null)
        {
            cached.Blocked = blocked;
            await cache.UpdateAsync(userId, cached);
        }
    }
}