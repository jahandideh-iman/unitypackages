
namespace Arman.Foundation.Core.ConfigurationManagement
{
    public  interface IConfigurer
    {
        void RegisterSelf(IConfigurationManager manager);
    }

    public interface IConfigurer<T> : IConfigurer
    {
        void Configure(T entity);

    }
}