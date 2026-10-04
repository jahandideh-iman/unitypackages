namespace Arman.DependencyResolution
{
    public interface IResolutionEntry<T>
    {
        public IResolutionEntry<T> As<U>();
    }
}
