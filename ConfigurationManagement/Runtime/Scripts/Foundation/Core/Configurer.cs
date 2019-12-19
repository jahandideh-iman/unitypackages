
namespace Match3.Foundation.Base.Configuration
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