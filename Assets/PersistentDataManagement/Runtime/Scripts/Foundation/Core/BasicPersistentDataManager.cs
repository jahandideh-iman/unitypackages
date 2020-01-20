using System.Collections.Generic;
using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class BasicPersistentDataManager : PersistentDataManager
    {
        PersistentDataWrapper persistentDataWrapper;
        Container<PersistentDataSerializer> allSerializers = new BasicContainer<PersistentDataSerializer>();

        Dictionary<Channel, Container<PersistentDataSerializer>> channelSerializers = new Dictionary<Channel, Container<PersistentDataSerializer>>();

        public void SetPersistentDataHandler(PersistentDataWrapper handler)
        {
            this.persistentDataWrapper = handler;
        }

        public void Register(PersistentDataSerializer serializer)
        {
            allSerializers.Add(serializer);
        }

        public void Register(PersistentDataSerializer serializer, Channel channel)
        {
            if (IsSerializerRegisted(serializer))
                throw new PersistentDataSerializerAlreadyRegisterException(serializer);

            TryCreateChannelData(channel);

            channelSerializers[channel].Add(serializer);
            allSerializers.Add(serializer);
        }

        private bool IsSerializerRegisted(PersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        public bool Contains(PersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        public void SaveAll()
        {
            foreach (var serializer in allSerializers.Items())
                serializer.Serialize(persistentDataWrapper);
        }

        public void Save(Channel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            foreach (var serializer in channelSerializers[channel].Items())
                serializer.Serialize(persistentDataWrapper);
        }

        private bool ChannelDoesNotExists(Channel channel)
        {
            return channelSerializers.ContainsKey(channel) == false;
        }

        private void TryCreateChannelData(Channel channel)
        {
            if (ChannelDoesNotExists(channel))
                channelSerializers.Add(channel, new BasicContainer<PersistentDataSerializer>());
        }
    }

}