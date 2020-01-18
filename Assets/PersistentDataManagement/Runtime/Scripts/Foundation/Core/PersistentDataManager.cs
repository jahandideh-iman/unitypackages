
using System;
using System.Collections.Generic;
using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataManager
    {

        void SetPersistentDataHandler(PersistentDataHandler handler);

        void Register(PersistentDataSerializer serializer);

        void Register(PersistentDataSerializer serializer, Channel channel);

        bool Contains(PersistentDataSerializer serializer);

        void Save(Channel channel);
        void SaveAll();
    }

    public interface PersistentDataHandler
    {
        void SaveInt(string key, int value);
        void SaveString(string key, string value);
    }

    public interface PersistentDataSerializer
    {
        void Serialize();
    }

 
    public class BasicPersistentDataManager : PersistentDataManager
    {
        PersistentDataHandler persistentDataHandler;
        Container<PersistentDataSerializer> allSerializers = new BasicContainer<PersistentDataSerializer>();

        Dictionary<Channel, Container<PersistentDataSerializer>> channelSerializers = new Dictionary<Channel, Container<PersistentDataSerializer>>();

        public void SetPersistentDataHandler(PersistentDataHandler handler)
        {
            this.persistentDataHandler = handler;
        }

        public void Register(PersistentDataSerializer serializer)
        {
            allSerializers.Add(serializer);
        }

        public void Register(PersistentDataSerializer serializer, Channel channel)
        {
            TryCreateChannelData(channel);

            channelSerializers[channel].Add(serializer);
            allSerializers.Add(serializer);
        }

        public bool Contains(PersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        public void SaveAll()
        {
            foreach (var serializer in allSerializers.Items())
                serializer.Serialize();
        }

        public void Save(Channel channel)
        {
            foreach (var serializer in channelSerializers[channel].Items())
                serializer.Serialize();
        }

        private void TryCreateChannelData(Channel channel)
        {
            if (channelSerializers.ContainsKey(channel) == false)
                channelSerializers.Add(channel, new BasicContainer<PersistentDataSerializer>());
        }
    }

}