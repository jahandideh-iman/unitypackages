
namespace Arman.ObjectPooling.Core
{
    public interface IPoolable
    {
        void OnAcquired();
        void OnReleased();
    }
}