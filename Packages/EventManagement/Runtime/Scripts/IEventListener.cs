
namespace Arman.EventManagement
{
    public interface IEventListener
    {
        void OnEvent(IGameEvent evt, object sender);
    }
}