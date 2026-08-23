
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.Foundation.EventManagement
{
    public interface IEventManager : IService
    {
        void Propagate(IGameEvent evt, object sender);
        void Register(IEventListener listener);
        void UnRegister(IEventListener listener);
        bool Has(IEventListener listener);
        void Clear();
    }
}