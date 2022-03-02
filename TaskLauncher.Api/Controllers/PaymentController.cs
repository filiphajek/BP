using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL.Repositories;

namespace TaskLauncher.Api.Controllers;

[Authorize(Policy = "user-policy")]
public class PaymentController : BaseController
{
    private readonly IMapper mapper;
    private readonly IPaymentRepository paymentRepository;

    public PaymentController(ILogger<PaymentController> logger, IMapper mapper, IPaymentRepository paymentRepository) : base(logger)
    {
        this.mapper = mapper;
        this.paymentRepository = paymentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentResponse>>> GetAllPayments()
    {
        var list = await paymentRepository.GetAllAsync();
        return Ok(list.Select(mapper.Map<PaymentResponse>));
    }
}
