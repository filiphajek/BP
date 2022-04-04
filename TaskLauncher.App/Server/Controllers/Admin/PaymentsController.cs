using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.Admin;

/// <summary>
/// Payment kontroler ke kteremu ma pristup pouze admin
/// </summary>
public class PaymentsController : AdminODataController<PaymentResponse>
{
    public PaymentsController(AppDbContext context) : base(context) { }

    /// <summary>
    /// Zobrazi vsechny platby v systemu, muze se dotazovat pres protokol odata
    /// </summary>
    public override ActionResult<IQueryable<PaymentResponse>> Get()
    {
        return Ok(context.Payments.IgnoreQueryFilters().ProjectToType<PaymentResponse>());
    }
}
