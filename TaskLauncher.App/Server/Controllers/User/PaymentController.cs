using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.User;

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
