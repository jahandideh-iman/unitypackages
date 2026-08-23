
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.Foundation.Core.ConfigurationManagement
{
    public interface IConfigurationManager : IService
    {
        void Register<T>(IConfigurer<T> configurer);
        IConfigurer<T> FindConfigurer<T>();

        bool Contains<T>(IConfigurer<T> configurer);

        void Configure<T>(T target);
        IConfigurer<T> RemoveConfigurer<T>();
    }
}