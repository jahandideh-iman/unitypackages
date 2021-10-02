
using Arman.Utility.Core;
using System;
using System.Collections.Generic;


// WARNING: Cycles in channel relations are not check. Having cycles may cause infinite lopps.
// TODO: Add a root channel.
// TODO: Check for cycles in channel relations.
// TODO: Refactor this.
namespace Arman.UpdateManagement.Foundation
{
    public delegate void ChannelStateChanged(Channel channel, bool isPaused);
    public class BasicUpdateManager : UpdateManager
    {
        public event ChannelStateChanged ChannelStateChangedEvent = delegate { };

        class ChannelData
        {
            public Channel channel;
            public ChannelData parrent;
            public float timeScale;
            public bool isPaused;
            public List<Updatable> updatables;

            public void AddUpdatable(Updatable updatable)
            {
                updatables.Add(updatable);
            }

            public void RemoveUpdatable(Updatable updatable)
            {
                updatables.RemoveAll(u => updatable.Equals(u));
            }
        }


        Dictionary<Channel, ChannelData> channelsData = new Dictionary<Channel, ChannelData>();

        List<Updatable> updatablesTemp = new List<Updatable>();

        public void RegisterChannel(Channel channel)
        {
            AddChannelDataIfIsNew(channel);
        }

        public void RegisterChannelToParent(Channel child, Channel parent)
        {
            AddChannelDataIfIsNew(child);
            ChannelDataFor(child).parrent = ChannelDataFor(parent);
        }

        public void RegisterUpdatable(Updatable updatable, Channel channel)
        {
            AddChannelDataIfIsNew(channel);
            ChannelDataFor(channel).AddUpdatable(updatable);
        }

        public void UnRegisterUpdatable(Updatable updatable)
        {
            var data = ChannelDataFor(updatable);
            if (data == null)
                return;

            data.RemoveUpdatable(updatable);
        }

        public void Pause(Channel channel)
        {
            if (HasChannel(channel))
            {
                ChannelDataFor(channel).isPaused = true;
                ChannelStateChangedEvent.Invoke(channel, true);
            }
        }

        public void Resume(Channel channel)
        {
            if (HasChannel(channel))
            {
                ChannelDataFor(channel).isPaused = false;
                ChannelStateChangedEvent.Invoke(channel, false);

            }
        }

        public void SetChannelTimeScale(Channel channel, float scale)
        {
            // NOTE: The effects of parent's time scales must be considered.
            throw new NotImplementedException();

            //ChannelDataFor(channel).timeScale = scale;
        }

        public bool Has(Updatable updatable)
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

        public bool IsChannelGloballyPaused(Channel channel)
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

        private ChannelData ChannelDataFor(Channel channel)
        {
            return channelsData[channel];
        }

        private bool HasChannel(Channel channel)
        {
            return channelsData.ContainsKey(channel);
        }

        private ChannelData ChannelDataFor(Updatable updatable)
        {
            foreach (var data in channelsData.Values)
                if (data.updatables.Contains(updatable))
                    return data;

            return null;
        }

        private void AddChannelDataIfIsNew(Channel channel)
        {
            if (HasChannel(channel))
                return;

            channelsData.Add(channel, CreateDefaultChannelDataFor(channel));
        }

        private ChannelData CreateDefaultChannelDataFor(Channel channel)
        {
            return new ChannelData()
            {
                isPaused = false,
                timeScale = 1f,
                updatables = new List<Updatable>(),
                channel = channel
            };
        }

    }
}