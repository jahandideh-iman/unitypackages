using Arman.PackageBasics;
using UnityEngine;

namespace Arman.UpdateManagement
{
    public class UnityUpdateManager : MonoBehaviour, IUpdateManager
    {
        UpdateManager _internalManager = new UpdateManager();

        public event ChannelStateChanged ChannelStateChangedEvent
        {
            add => _internalManager.ChannelStateChangedEvent += value;
            remove => _internalManager.ChannelStateChangedEvent -= value;
        }

        private void Update()
        {
            _internalManager.AdvanceTime(Time.deltaTime);
        }

        public bool Has(IUpdatable updatable)
        {
            return _internalManager.Has(updatable);
        }

        public bool IsChannelGloballyPaused(IChannel channel)
        {
            return _internalManager.IsChannelGloballyPaused(channel);
        }

        public void Pause(IChannel channel)
        {
            _internalManager.Pause(channel);
        }

        public void RegisterChannel(IChannel channel)
        {
            _internalManager.RegisterChannel(channel);
        }

        public void RegisterChannelToParent(IChannel child, IChannel parent)
        {
            _internalManager.RegisterChannelToParent(child, parent);
        }

        public void RegisterUpdatable(IUpdatable updatable, IChannel channel)
        {
            _internalManager.RegisterUpdatable(updatable, channel);
        }

        public void Resume(IChannel channel)
        {
            _internalManager.Resume(channel);
        }

        public void SetChannelTimeScale(IChannel channel, float scale)
        {
            _internalManager.SetChannelTimeScale(channel, scale);
        }

        public void UnRegisterUpdatable(IUpdatable updatable)
        {
            _internalManager.UnRegisterUpdatable(updatable);
        }
    }
}
