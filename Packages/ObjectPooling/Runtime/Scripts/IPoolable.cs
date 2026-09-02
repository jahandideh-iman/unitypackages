
namespace Arman.ObjectPooling
{
    public interface IPoolable
    {
        void OnAcquired();
        void OnReleased();
    }
}