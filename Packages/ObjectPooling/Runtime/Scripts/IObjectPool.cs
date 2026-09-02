
namespace Arman.ObjectPooling
{
    public interface IObjectPool<T> where T: IPoolable
    {
        T Acquire();
        void Release(T obj);

        void Reserve(int count);

        int Size();

    }
}