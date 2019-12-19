


using Match3.Foundation.Base.ServiceLocating;

namespace Match3.Foundation.Base.Configuration
{
    public interface ConfigurationManager : Service
    {
        void Register<T>(Configurer<T> configurer);
        Configurer<T> FindConfigurer<T>();

        bool Contains<T>(Configurer<T> configurer);

        void Configure<T>(T target);
        Configurer<T> RemoveConfigurer<T>();
    }
}