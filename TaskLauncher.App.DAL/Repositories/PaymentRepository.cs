using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL.Repositories.Base;

namespace TaskLauncher.App.DAL.Repositories;

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