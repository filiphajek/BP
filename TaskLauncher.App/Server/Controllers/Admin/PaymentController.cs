using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.Admin;

public class PaymentController : AdminODataController<PaymentResponse>
{
    public PaymentController(AppDbContext context) : base(context) { }

    public override ActionResult<IQueryable<PaymentResponse>> Get()
    {
        return Ok(context.Payments.IgnoreQueryFilters().ProjectToType<PaymentResponse>());
    }
}
