
namespace Arman.Foundation.Core.ConfigurationManagement
{
    public  interface Configurer
    {
        void RegisterSelf(ConfigurationManager manager);
    }

    public interface Configurer<T> : Configurer
    {
        void Configure(T entity);

    }
}