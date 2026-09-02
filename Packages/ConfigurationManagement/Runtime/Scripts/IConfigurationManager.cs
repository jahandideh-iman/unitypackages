
namespace Arman.ConfigurationManagement
{
    public interface IConfigurationManager
    {
        void Register<T>(IConfigurer<T> configurer);
        IConfigurer<T> FindConfigurer<T>();

        bool Contains<T>(IConfigurer<T> configurer);

        void Configure<T>(T target);
        IConfigurer<T> RemoveConfigurer<T>();
    }
}