using Microsoft.Extensions.DependencyInjection;

namespace ModuleBased.Core.Infrastructure
{
    public interface IModuleInitializer
    {
        void Init(IServiceCollection serviceCollection);
    }
}
