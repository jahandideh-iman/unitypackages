
using Arman.Utility.Core;
using System;
using System.Collections.Generic;


// WARNING: Cycles in channel relations are not check. Having cycles may cause infinite lopps.
// TODO: Add a root channel.
// TODO: Check for cycles in channel relations.
// TODO: Refactor this.
namespace Arman.UpdateManagement.Foundation
{
    public delegate void ChannelStateChanged(IChannel channel, bool isPaused);
    public class BasicUpdateManager : IUpdateManager
    {
        public event ChannelStateChanged ChannelStateChangedEvent = delegate { };

        class ChannelData
        {
            public IChannel channel;
            public ChannelData parrent;
            public float timeScale;
            public bool isPaused;
            public List<IUpdatable> updatables;

            public void AddUpdatable(IUpdatable updatable)
            {
                updatables.Add(updatable);
            }

            public void RemoveUpdatable(IUpdatable updatable)
            {
                updatables.RemoveAll(u => updatable.Equals(u));
            }
        }


        Dictionary<IChannel, ChannelData> channelsData = new Dictionary<IChannel, ChannelData>();

        List<IUpdatable> updatablesTemp = new List<IUpdatable>();

        public void RegisterChannel(IChannel channel)
        {
            AddChannelDataIfIsNew(channel);
        }

        public void RegisterChannelToParent(IChannel child, IChannel parent)
        {
            AddChannelDataIfIsNew(child);
            ChannelDataFor(child).parrent = ChannelDataFor(parent);
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
                ChannelDataFor(channel).isPaused = true;
                ChannelStateChangedEvent.Invoke(channel, true);
            }
        }

        public void Resume(IChannel channel)
        {
            if (HasChannel(channel))
            {
                ChannelDataFor(channel).isPaused = false;
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
            foreach (var data in channelsData.Values)
                if (data.updatables.Contains(updatable))
                    return true;
            return false;
        }

        public void AdvanceTime(float amount)
        {
            foreach (var data in channelsData.Values)
                AdvanceTimeFor(data, amount);

        }

        private void AdvanceTimeFor(ChannelData data, float amount)
        {
            if (IsChannelDataGloballyPaused(data))
                return ;

            // WARNING: This is costly. This is a fast (and hacky) soloution to handle 
            // changes to updatables while iterating (due to unregistering).
            updatablesTemp.Clear();
            updatablesTemp.AddRange(data.updatables); 

            var count = updatablesTemp.Count;
            for (int i = count-1; i >= 0; --i)
                updatablesTemp[i].UpdateTime(amount * data.timeScale);
        }

        public bool IsChannelGloballyPaused(IChannel channel)
        {
            return IsChannelDataGloballyPaused(ChannelDataFor(channel));
        }

        private bool IsChannelDataGloballyPaused(ChannelData data)
        {
            var current = data;

            while(current!= null)
            {
                if (current.isPaused)
                    return true;
                current = current.parrent;
            }

            return false;
        }

        private ChannelData ChannelDataFor(IChannel channel)
        {
            return channelsData[channel];
        }

        private bool HasChannel(IChannel channel)
        {
            return channelsData.ContainsKey(channel);
        }

        private ChannelData ChannelDataFor(IUpdatable updatable)
        {
            foreach (var data in channelsData.Values)
                if (data.updatables.Contains(updatable))
                    return data;

            return null;
        }

        private void AddChannelDataIfIsNew(IChannel channel)
        {
            if (HasChannel(channel))
                return;

            channelsData.Add(channel, CreateDefaultChannelDataFor(channel));
        }

        private ChannelData CreateDefaultChannelDataFor(IChannel channel)
        {
            return new ChannelData()
            {
                isPaused = false,
                timeScale = 1f,
                updatables = new List<IUpdatable>(),
                channel = channel
            };
        }

    }
}