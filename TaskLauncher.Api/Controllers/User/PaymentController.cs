using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.User;

public class PaymentController : UserODataController<PaymentResponse>
{
    public PaymentController(AppDbContext context) : base(context) { }

    [HttpGet]
    [EnableQuery]
    public ActionResult<PaymentResponse> Get()
    {
        return Ok(context.Payments.AsQueryable().ProjectToType<PaymentResponse>());
    }
}
