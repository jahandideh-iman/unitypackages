
using Arman.Utility.Core;
using UnityEngine;

namespace Arman.UpdateManagement.Foundation.Unity
{
    public class UnityUpdateManager : MonoBehaviour, UpdateManager
    {
        BasicUpdateManager internalManager = new BasicUpdateManager();


        private void Update()
        {
            internalManager.AdvanceTime(Time.deltaTime);
        }

        public bool Has(Updatable updatable)
        {
            return internalManager.Has(updatable);
        }

        public bool IsChannelGloballyPaused(Channel channel)
        {
            return internalManager.IsChannelGloballyPaused(channel);
        }

        public void Pause(Channel channel)
        {
            internalManager.Pause(channel);
        }

        public void RegisterChannel(Channel channel)
        {
            internalManager.RegisterChannel(channel);
        }

        public void RegisterChannelToParent(Channel child, Channel parent)
        {
            internalManager.RegisterChannelToParent(child, parent);
        }

        public void RegisterUpdatable(Updatable updatable, Channel channel)
        {
            internalManager.RegisterUpdatable(updatable, channel);
        }

        public void Resume(Channel channel)
        {
            internalManager.Resume(channel);
        }

        public void SetChannelTimeScale(Channel channel, float scale)
        {
            internalManager.SetChannelTimeScale(channel, scale);
        }

        public void UnRegisterUpdatable(Updatable updatable)
        {
            internalManager.UnRegisterUpdatable(updatable);
        }
    }
}