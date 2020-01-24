using System.Collections.Generic;
using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{

    using SerializerContainer = Container<PersistentDataSerializer>;

    public class BasicPersistentDataManager : PersistentDataManager
    {
        private class InternalChannel : Channel
        { }

        PersistentDataIOStreamFactory persistentDataIOStreamFactory;
        PersistentDataWrapper persistentDataWrapper;

        SerializerContainer allSerializers = new BasicContainer<PersistentDataSerializer>();
        Dictionary<Channel, SerializerContainer> channelSerializers = new Dictionary<Channel, SerializerContainer>();

        Channel defaultChannel = new InternalChannel();

        public void SetPersistentDataHandler(PersistentDataWrapper handler)
        {
            this.persistentDataWrapper = handler;
        }

        public void SetPersistentDataIOStreamFactory(PersistentDataIOStreamFactory factory)
        {
            this.persistentDataIOStreamFactory = factory;
        }

        public void Register(PersistentDataSerializer serializer)
        {
            Register(serializer, defaultChannel);
        }

        public void Register(PersistentDataSerializer serializer, Channel channel)
        {
            if (IsSerializerRegistered(serializer))
                throw new PersistentDataSerializerAlreadyRegisterException(serializer);

            TryCreateChannelData(channel);

            channelSerializers[channel].Add(serializer);
            allSerializers.Add(serializer);
        }

        public void SaveAll()
        {
            foreach (var channel in channelSerializers.Keys)
                Save(channel);
        }

        public void Save(Channel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            persistentDataWrapper.Clear();

            foreach (var serializer in channelSerializers[channel].Items())
                serializer.Serialize(persistentDataWrapper);

            persistentDataWrapper.WriteTo(persistentDataIOStreamFactory.CreateWriteStreamFor(channel));
        }

        public bool Contains(PersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        private bool IsSerializerRegistered(PersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
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