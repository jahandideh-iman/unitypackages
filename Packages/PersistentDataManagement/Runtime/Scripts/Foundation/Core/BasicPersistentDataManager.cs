using System.Collections.Generic;
using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    using System;
    using SerializerContainer = Container<PersistentDataSerializer>;

    public class BasicPersistentDataManager : PersistentDataManager
    {
        // NOTE: The name (ToString) of the channel must never be used by other channel.
        // TODO: Find a way to guarantee that it never can happen.
        private class InternalChannel : Channel
        {
            public override string ToString()
            {
                return "_internal";
            }
        }

        PersistentDataIOStreamFactory persistentDataIOStreamFactory;
        PersistentDataWrapper persistentDataWrapper;

        SerializerContainer allSerializers = new BasicContainer<PersistentDataSerializer>();
        Dictionary<Channel, SerializerContainer> channelSerializers = new Dictionary<Channel, SerializerContainer>();

        Channel defaultChannel = new InternalChannel();

        int saveVersion;

        public BasicPersistentDataManager()
        {
        }

        public BasicPersistentDataManager(
            PersistentDataIOStreamFactory persistentDataIOStreamFactory,
            PersistentDataWrapper persistentDataWrapper, 
            int saveVersion)
        {
            this.persistentDataIOStreamFactory = persistentDataIOStreamFactory;
            this.persistentDataWrapper = persistentDataWrapper;
            this.saveVersion = saveVersion;
        }

        public void SetSaveVersion(int version)
        {
            this.saveVersion = version;
        }

        public void SetPersistentDataWrapper(PersistentDataWrapper wrapper)
        {
            this.persistentDataWrapper = wrapper;
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

            WriteMetaDataTo(persistentDataWrapper);
            WriteDataTo(persistentDataWrapper, channelSerializers[channel].Items());

            using (var writeStream = persistentDataIOStreamFactory.CreateWriteStreamFor(channel))
                persistentDataWrapper.WriteTo(writeStream);
            
        }

        void WriteMetaDataTo(WritablePersistentDataWrapper dataWrapper)
        {
            dataWrapper.BeginWritingBlock("MetaData");

            dataWrapper.WriteInt("Version", saveVersion);

            dataWrapper.EndWritingBlock();
        }

        private void WriteDataTo(PersistentDataWrapper persistentDataWrapper, IEnumerable<PersistentDataSerializer> serializers)
        {
            persistentDataWrapper.BeginWritingBlock("Data");

            foreach (var serializer in serializers)
                Serialize(serializer, persistentDataWrapper);


            persistentDataWrapper.EndWritingBlock();
        }

        private void Serialize(PersistentDataSerializer serializer, WritablePersistentDataWrapper persistentDataWrapper)
        {
            persistentDataWrapper.BeginWritingBlock(serializer.Key());
            serializer.SerializeTo(persistentDataWrapper);
            persistentDataWrapper.EndWritingBlock();
        }

        public void LoadAll()
        {
            foreach (var channel in channelSerializers.Keys)
                Load(channel);
        }

        public void Load(Channel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            if (persistentDataIOStreamFactory.HasReadableStreamFor(channel) == false)
                return;

            using (var readStream = persistentDataIOStreamFactory.CreateReadStreamFor(channel))
            {
                persistentDataWrapper.Clear();
                persistentDataWrapper.ReadFrom(readStream);

                persistentDataWrapper.BeginReadingBlock("Data");

                foreach (var serializer in channelSerializers[channel].Items())
                    TryDeserialize(serializer, persistentDataWrapper);

                persistentDataWrapper.EndReadingBlock();
            }
        }

        private void TryDeserialize(PersistentDataSerializer serializer, ReadablePersistentDataWrapper persistentDataWrapper)
        {
            if (persistentDataWrapper.HasKey(serializer.Key()))
            {
                persistentDataWrapper.BeginReadingBlock(serializer.Key());
                serializer.DeserializeFrom(persistentDataWrapper);
                persistentDataWrapper.EndReadingBlock();
            }
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