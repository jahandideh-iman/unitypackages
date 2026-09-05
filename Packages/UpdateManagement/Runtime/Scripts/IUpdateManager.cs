


using Arman.PackageBasics;

namespace Arman.UpdateManagement
{
    public delegate void ChannelStateChanged(IChannel channel, bool isPaused);

    public interface IUpdateManager
    {
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