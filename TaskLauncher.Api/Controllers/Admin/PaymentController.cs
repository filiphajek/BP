using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.Admin;

public class PaymentController : AdminODataController<PaymentResponse>
{
    public PaymentController(AppDbContext context) : base(context) { }

    public override ActionResult<IQueryable<PaymentResponse>> Get()
    {
        return Ok(context.Payments.IgnoreQueryFilters().ProjectToType<PaymentResponse>());
    }
}
