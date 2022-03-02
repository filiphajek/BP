using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories.Base;

namespace TaskLauncher.Api.DAL.Repositories;

public interface IPaymentRepository : IBaseRepository<PaymentEntity>
{
}

public class PaymentRepository : AppRepository<PaymentEntity>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<PaymentEntity>> GetAllAsync() 
        => await Context.Payments.Include(i => i.Task).ToListAsync();
}