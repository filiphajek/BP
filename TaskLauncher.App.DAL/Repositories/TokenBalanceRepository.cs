using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL.Repositories.Base;

namespace TaskLauncher.App.DAL.Repositories;

public interface ITokenBalanceRepository : IBaseRepository<TokenBalanceEntity>
{
}

public class TokenBalanceRepository : AppRepository<TokenBalanceEntity>, ITokenBalanceRepository
{
    public TokenBalanceRepository(AppDbContext context) : base(context)
    {
    }
}
