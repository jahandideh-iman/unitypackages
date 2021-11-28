
namespace Arman.ObjectPooling.Core
{
    public interface ObjectPool<T> where T: Poolable
    {
        T Acquire();
        void Release(T obj);

        void Reserve(int count);

        int Size();

    }
}