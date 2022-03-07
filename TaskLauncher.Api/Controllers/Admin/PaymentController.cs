using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.Controllers.Admin;

public class PaymentController : AdminODataController<PaymentEntity>
{
    public PaymentController(AppDbContext context) : base(context) { }

    public override ActionResult<PaymentEntity> Get(string userId = "") //https://localhost:5001/test/adminpayment?userid=6225224ff0bca300691d9bd8&$select=id
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(context.Payments.IgnoreQueryFilters());
        }
        return Ok(context.Payments.IgnoreQueryFilters().Where(i => i.UserId == userId));
    }
}
