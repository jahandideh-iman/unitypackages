
namespace Arman.EventManagement
{
    public interface IEventManager
    {
        void Propagate(IGameEvent evt, object sender);
        void Register(IEventListener listener);
        void UnRegister(IEventListener listener);
        bool Has(IEventListener listener);
        void Clear();
    }
}