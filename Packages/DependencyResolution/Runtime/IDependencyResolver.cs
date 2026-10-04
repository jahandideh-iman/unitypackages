using System;

namespace Arman.DependencyResolution
{
    public interface IDependencyResolver
    {
        IResolutionEntry<T> RegisterType<T>();
        IResolutionEntry<T> RegisterFactory<T>(Delegate factory);
        IResolutionEntry<T> RegisterInstance<T>(T instance);

        IRepository Build();
    }
}
