
using Arman.PackageBasics;
using UnityEngine;

namespace Arman.UpdateManagement
{
    public class UnityUpdateManager : MonoBehaviour, IUpdateManager
    {
        BasicUpdateManager internalManager = new BasicUpdateManager();

        public event ChannelStateChanged ChannelStateChangedEvent
        {
            add => internalManager.ChannelStateChangedEvent += value;
            remove => internalManager.ChannelStateChangedEvent -= value;
        }

        private void Update()
        {
            internalManager.AdvanceTime(Time.deltaTime);
        }

        public bool Has(IUpdatable updatable)
        {
            return internalManager.Has(updatable);
        }

        public bool IsChannelGloballyPaused(IChannel channel)
        {
            return internalManager.IsChannelGloballyPaused(channel);
        }

        public void Pause(IChannel channel)
        {
            internalManager.Pause(channel);
        }

        public void RegisterChannel(IChannel channel)
        {
            internalManager.RegisterChannel(channel);
        }

        public void RegisterChannelToParent(IChannel child, IChannel parent)
        {
            internalManager.RegisterChannelToParent(child, parent);
        }

        public void RegisterUpdatable(IUpdatable updatable, IChannel channel)
        {
            internalManager.RegisterUpdatable(updatable, channel);
        }

        public void Resume(IChannel channel)
        {
            internalManager.Resume(channel);
        }

        public void SetChannelTimeScale(IChannel channel, float scale)
        {
            internalManager.SetChannelTimeScale(channel, scale);
        }

        public void UnRegisterUpdatable(IUpdatable updatable)
        {
            internalManager.UnRegisterUpdatable(updatable);
        }
    }
}