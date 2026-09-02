


using Arman.PackageBasics;

namespace Arman.UpdateManagement
{

    public interface IUpdateManager
    {
        // Declared here, not only on BasicUpdateManager: consumers receive an
        // IUpdateManager (that is all a Service Locator hands out), so an event that lives
        // on the concrete type alone is unreachable to every one of them.
        event ChannelStateChanged ChannelStateChangedEvent;

        void RegisterChannel(IChannel channel);
        void RegisterChannelToParent(IChannel child, IChannel parent);

        void RegisterUpdatable(IUpdatable updatable, IChannel channel);
        void UnRegisterUpdatable(IUpdatable updatable);

        void Pause(IChannel channel);
        void Resume(IChannel channel);

        void SetChannelTimeScale(IChannel channel, float scale);

        bool Has(IUpdatable updatable);
        bool IsChannelGloballyPaused(IChannel channel);

    }
}