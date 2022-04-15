using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.User;

/// <summary>
/// Payment kontroler ke kteremu ma pristup pouze uzivatel
/// </summary>
public class PaymentsController : UserODataController<PaymentResponse>
{
    public PaymentsController(AppDbContext context) : base(context) { }

    /// <summary>
    /// Vrací všechny platby uživatele, dotazuje se přes protokol odata
    /// </summary>
    [ProducesResponseType(typeof(List<PaymentResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    [EnableQuery]
    public ActionResult<PaymentResponse> Get()
    {
        return Ok(context.Payments.AsQueryable().ProjectToType<PaymentResponse>());
    }
}
