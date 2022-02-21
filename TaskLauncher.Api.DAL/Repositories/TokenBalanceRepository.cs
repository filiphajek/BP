using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories.Base;

namespace TaskLauncher.Api.DAL.Repositories;

public interface ITokenBalanceRepository : IBaseRepository<TokenBalanceEntity>
{
}

public class TokenBalanceRepository : AppRepository<TokenBalanceEntity>, ITokenBalanceRepository
{
    public TokenBalanceRepository(AppDbContext context) : base(context)
    {
    }
}
