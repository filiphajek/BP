using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
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
    /// Vrací všechny platby v systému, dotazuje se přes odata
    /// </summary>
    [ProducesResponseType(typeof(List<PaymentResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    [EnableQuery]
    public ActionResult<IQueryable<PaymentResponse>> Get()
    {
        return Ok(context.Payments.IgnoreQueryFilters().ProjectToType<PaymentResponse>());
    }
}
