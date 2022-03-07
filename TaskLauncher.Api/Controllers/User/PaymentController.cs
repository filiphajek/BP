using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.Controllers.User;

public class PaymentController : UserODataController<PaymentEntity>
{
    public PaymentController(AppDbContext context) : base(context) { }

    [HttpGet]
    [EnableQuery]
    public ActionResult<PaymentEntity> Get()
    {
        return Ok(context.Payments.AsQueryable());
    }
}
