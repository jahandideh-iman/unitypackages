
namespace Arman.Foundation.EventManagement
{
    public interface IEventListener
    {
        void OnEvent(IGameEvent evt, object sender);
    }
}