using System.Collections.Generic;
using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    using System;
    using SerializerContainer = IContainer<IPersistentDataSerializer>;

    public class PersistentDataManager : IPersistentDataManager
    {
        // NOTE: The name (ToString) of the channel must never be used by other channel.
        // TODO: Find a way to guarantee that it never can happen.
        private class InternalChannel : IChannel
        {
            public override string ToString()
            {
                return "_internal";
            }
        }

        IPersistentDataIOStreamFactory persistentDataIOStreamFactory;
        IPersistentDataWrapper persistentDataWrapper;

        SerializerContainer allSerializers = new Container<IPersistentDataSerializer>();
        Dictionary<IChannel, SerializerContainer> channelSerializers = new Dictionary<IChannel, SerializerContainer>();

        IChannel defaultChannel = new InternalChannel();

        int saveVersion;

        public PersistentDataManager(
            IPersistentDataIOStreamFactory persistentDataIOStreamFactory,
            IPersistentDataWrapper persistentDataWrapper, 
            int saveVersion)
        {
            this.persistentDataIOStreamFactory = persistentDataIOStreamFactory;
            this.persistentDataWrapper = persistentDataWrapper;
            this.saveVersion = saveVersion;
        }

        public void Register(IPersistentDataSerializer serializer)
        {
            Register(serializer, defaultChannel);
        }

        public void Register(IPersistentDataSerializer serializer, IChannel channel)
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

        public void Save(IChannel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            persistentDataWrapper.Clear();

            WriteMetaDataTo(persistentDataWrapper);
            WriteDataTo(persistentDataWrapper, channelSerializers[channel].Items());

            using (var writeStream = persistentDataIOStreamFactory.CreateWriteStreamFor(channel))
                persistentDataWrapper.WriteTo(writeStream);
            
        }

        void WriteMetaDataTo(IWritablePersistentDataWrapper dataWrapper)
        {
            dataWrapper.BeginWritingBlock("MetaData");

            dataWrapper.WriteInt("Version", saveVersion);

            dataWrapper.EndWritingBlock();
        }

        private void WriteDataTo(IPersistentDataWrapper persistentDataWrapper, IEnumerable<IPersistentDataSerializer> serializers)
        {
            persistentDataWrapper.BeginWritingBlock("Data");

            foreach (var serializer in serializers)
                Serialize(serializer, persistentDataWrapper);


            persistentDataWrapper.EndWritingBlock();
        }

        private void Serialize(IPersistentDataSerializer serializer, IWritablePersistentDataWrapper persistentDataWrapper)
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

        public void Load(IChannel channel)
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

        public void Delete(IChannel channel)
        {
            persistentDataIOStreamFactory.Delete(channel);
        }

        private void TryDeserialize(IPersistentDataSerializer serializer, IReadablePersistentDataWrapper persistentDataWrapper)
        {
            if (persistentDataWrapper.HasKey(serializer.Key()))
            {
                persistentDataWrapper.BeginReadingBlock(serializer.Key());
                serializer.DeserializeFrom(persistentDataWrapper);
                persistentDataWrapper.EndReadingBlock();
            }
        }

        public bool Contains(IPersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        private bool IsSerializerRegistered(IPersistentDataSerializer serializer)
        {
            return allSerializers.Contains(serializer);
        }

        private bool ChannelDoesNotExists(IChannel channel)
        {
            return channelSerializers.ContainsKey(channel) == false;
        }

        private void TryCreateChannelData(IChannel channel)
        {
            if (ChannelDoesNotExists(channel))
                channelSerializers.Add(channel, new Container<IPersistentDataSerializer>());
        }

    }

}