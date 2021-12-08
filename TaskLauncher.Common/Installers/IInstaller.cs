using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Common.Installers;

public interface IInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}
