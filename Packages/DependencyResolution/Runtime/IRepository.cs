namespace Arman.DependencyResolution
{
    public interface IRepository
    {
        T Get<T>();
        bool TryGet<T>(out T instance);
    }
}
