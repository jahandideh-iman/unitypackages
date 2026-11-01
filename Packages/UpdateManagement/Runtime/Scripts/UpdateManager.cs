using System;
using System.Collections.Generic;
using Arman.PackageBasics;
using UnityEngine;

// WARNING: Cycles in channel relations are not check. Having cycles may cause infinite lopps.
// TODO: Add a root channel.
// TODO: Check for cycles in channel relations.
// TODO: Refactor this.
namespace Arman.UpdateManagement
{
    public class UpdateManager : IUpdateManager
    {
        public event ChannelStateChanged ChannelStateChangedEvent = delegate { };

        class ChannelData
        {
            public IChannel Channel;
            public ChannelData Parent;
            public float TimeScale;
            public bool IsPaused;
            public List<IUpdatable> Updatables;

            public void AddUpdatable(IUpdatable updatable)
            {
                Updatables.Add(updatable);
            }

            public void RemoveUpdatable(IUpdatable updatable)
            {
                Updatables.RemoveAll(u => updatable.Equals(u));
            }
        }

        Dictionary<IChannel, ChannelData> _channelsData = new Dictionary<IChannel, ChannelData>();

        List<IUpdatable> _updatablesTemp = new List<IUpdatable>();

        public void RegisterChannel(IChannel channel)
        {
            AddChannelDataIfIsNew(channel);
        }

        public void RegisterChannelToParent(IChannel child, IChannel parent)
        {
            AddChannelDataIfIsNew(child);
            ChannelDataFor(child).Parent = ChannelDataFor(parent);
        }

        public void RegisterUpdatable(IUpdatable updatable, IChannel channel)
        {
            AddChannelDataIfIsNew(channel);
            ChannelDataFor(channel).AddUpdatable(updatable);
        }

        public void UnRegisterUpdatable(IUpdatable updatable)
        {
            var data = ChannelDataFor(updatable);
            if (data == null)
                return;

            data.RemoveUpdatable(updatable);
        }

        public void Pause(IChannel channel)
        {
            if (HasChannel(channel))
            {
                ChannelDataFor(channel).IsPaused = true;
                ChannelStateChangedEvent.Invoke(channel, true);
            }
        }

        public void Resume(IChannel channel)
        {
            if (HasChannel(channel))
            {
                ChannelDataFor(channel).IsPaused = false;
                ChannelStateChangedEvent.Invoke(channel, false);
            }
        }

        public void SetChannelTimeScale(IChannel channel, float scale)
        {
            // NOTE: The effects of parent's time scales must be considered.
            throw new NotImplementedException();

            //ChannelDataFor(channel).timeScale = scale;
        }

        public bool Has(IUpdatable updatable)
        {
            foreach (var data in _channelsData.Values)
                if (data.Updatables.Contains(updatable))
                    return true;
            return false;
        }

        public void AdvanceTime(float amount)
        {
            foreach (var data in _channelsData.Values)
                AdvanceTimeFor(data, amount);
        }

        private void AdvanceTimeFor(ChannelData data, float amount)
        {
            if (IsChannelDataGloballyPaused(data))
                return;

            // WARNING: This is costly. This is a fast (and hacky) solution to handle
            // changes to updatables while iterating (due to unregistering).
            _updatablesTemp.Clear();
            _updatablesTemp.AddRange(data.Updatables);

            var count = _updatablesTemp.Count;
            for (int i = count - 1; i >= 0; --i)
            {
                // An updatable may pause its own channel, so the state is checked for every updatable.
                if (IsChannelDataGloballyPaused(data))
                    return;

                try
                {
                    _updatablesTemp[i].UpdateTime(amount * data.TimeScale);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public bool IsChannelGloballyPaused(IChannel channel)
        {
            return IsChannelDataGloballyPaused(ChannelDataFor(channel));
        }

        private bool IsChannelDataGloballyPaused(ChannelData data)
        {
            var current = data;

            while (current != null)
            {
                if (current.IsPaused)
                    return true;
                current = current.Parent;
            }

            return false;
        }

        private ChannelData ChannelDataFor(IChannel channel)
        {
            return _channelsData[channel];
        }

        private bool HasChannel(IChannel channel)
        {
            return _channelsData.ContainsKey(channel);
        }

        private ChannelData ChannelDataFor(IUpdatable updatable)
        {
            foreach (var data in _channelsData.Values)
                if (data.Updatables.Contains(updatable))
                    return data;

            return null;
        }

        private void AddChannelDataIfIsNew(IChannel channel)
        {
            if (HasChannel(channel))
                return;

            _channelsData.Add(channel, CreateDefaultChannelDataFor(channel));
        }

        private ChannelData CreateDefaultChannelDataFor(IChannel channel)
        {
            return new ChannelData()
            {
                IsPaused = false,
                TimeScale = 1f,
                Updatables = new List<IUpdatable>(),
                Channel = channel,
            };
        }
    }
}
