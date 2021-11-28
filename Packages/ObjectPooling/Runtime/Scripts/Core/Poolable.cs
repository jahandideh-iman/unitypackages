
namespace Arman.ObjectPooling.Core
{
    public interface Poolable
    {
        void OnAcquired();
        void OnReleased();
    }
}