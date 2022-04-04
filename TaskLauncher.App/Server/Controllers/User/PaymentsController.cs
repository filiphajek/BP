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
    /// Zobrazi vsechny uzivatelske platby v systemu, muze se dotazovat pres protokol odata
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public ActionResult<PaymentResponse> Get()
    {
        return Ok(context.Payments.AsQueryable().ProjectToType<PaymentResponse>());
    }
}
