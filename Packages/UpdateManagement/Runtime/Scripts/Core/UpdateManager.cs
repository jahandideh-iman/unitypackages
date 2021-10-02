


using Arman.Foundation.Core.ServiceLocating;
using Arman.Utility.Core;

namespace Arman.UpdateManagement.Foundation
{

    public interface UpdateManager : Service
    {
        void RegisterChannel(Channel channel);
        void RegisterChannelToParent(Channel child, Channel parent);

        void RegisterUpdatable(Updatable updatable, Channel channel);
        void UnRegisterUpdatable(Updatable updatable);

        void Pause(Channel channel);
        void Resume(Channel channel);

        void SetChannelTimeScale(Channel channel, float scale);

        bool Has(Updatable updatable);
        bool IsChannelGloballyPaused(Channel channel);

    }
}