using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.Admin;

public class BanController : AdminODataController<BanResponse>
{
    private readonly IMapper mapper;

    public BanController(AppDbContext context, IMapper mapper) : base(context)
    {
        this.mapper = mapper;
    }

    public override ActionResult<IQueryable<BanResponse>> Get(string userId = "")
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(context.Bans.IgnoreQueryFilters().ProjectToType<BanResponse>());
        }
        return Ok(context.Bans.IgnoreQueryFilters().Where(i => i.UserId == userId).ProjectToType<BanResponse>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BanResponse>> GetDetail(Guid id)
    {
        var ban = await context.Bans.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();
        return Ok(mapper.Map<BanResponse>(ban));
    }
}