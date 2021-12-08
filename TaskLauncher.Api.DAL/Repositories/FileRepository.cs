using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Api.DAL.Repositories.Base;

namespace TaskLauncher.Api.DAL.Repositories;

public interface IFileRepository : IBaseRepository<FileEntity>
{

}

public class FileRepository : AppRepository<FileEntity>, IFileRepository
{
    public FileRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<FileEntity?> GetAsync(FileEntity entity) 
        => await Context.Files.Include(i => i.Task).AsNoTracking().SingleOrDefaultAsync(i => i.Id == entity.Id);
}
