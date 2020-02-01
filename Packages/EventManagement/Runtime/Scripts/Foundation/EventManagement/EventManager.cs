
using Arman.Foundation.Core.ServiceLocating;

namespace Arman.Foundation.EventManagement
{
    public interface EventManager : Service
    {
        void Propagate(GameEvent evt, object sender);
        void Register(EventListener listener);
        void UnRegister(EventListener listener);
        bool Has(EventListener listener);
        void Clear();
    }
}