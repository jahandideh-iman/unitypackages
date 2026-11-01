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

        IPersistentDataIOStreamFactory _persistentDataIOStreamFactory;
        IPersistentDataEncoder _persistentDataEncoder;

        SerializerContainer _allSerializers = new Container<IPersistentDataSerializer>();
        Dictionary<IChannel, SerializerContainer> _channelSerializers =
            new Dictionary<IChannel, SerializerContainer>();

        IChannel _defaultChannel = new InternalChannel();

        int _saveVersion;

        public PersistentDataManager(
            IPersistentDataIOStreamFactory persistentDataIOStreamFactory,
            IPersistentDataEncoder persistentDataEncoder,
            int saveVersion
        )
        {
            this._persistentDataIOStreamFactory = persistentDataIOStreamFactory;
            this._persistentDataEncoder = persistentDataEncoder;
            this._saveVersion = saveVersion;
        }

        public void Register(IPersistentDataSerializer serializer)
        {
            Register(serializer, _defaultChannel);
        }

        public void Register(IPersistentDataSerializer serializer, IChannel channel)
        {
            if (IsSerializerRegistered(serializer))
                throw new PersistentDataSerializerAlreadyRegisterException(serializer);

            TryCreateChannelData(channel);

            _channelSerializers[channel].Add(serializer);
            _allSerializers.Add(serializer);
        }

        public void SaveAll()
        {
            foreach (var channel in _channelSerializers.Keys)
                Save(channel);
        }

        public void Save(IChannel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            _persistentDataEncoder.Clear();

            WriteMetaDataTo(_persistentDataEncoder);
            WriteDataTo(_persistentDataEncoder, _channelSerializers[channel].Items());

            using (var writeStream = _persistentDataIOStreamFactory.CreateWriteStreamFor(channel))
                _persistentDataEncoder.WriteTo(writeStream);
        }

        void WriteMetaDataTo(IWritablePersistentDataEncoder dataEncoder)
        {
            dataEncoder.BeginWritingBlock("MetaData");

            dataEncoder.WriteInt("Version", _saveVersion);

            dataEncoder.EndWritingBlock();
        }

        private void WriteDataTo(
            IPersistentDataEncoder persistentDataEncoder,
            IEnumerable<IPersistentDataSerializer> serializers
        )
        {
            persistentDataEncoder.BeginWritingBlock("Data");

            foreach (var serializer in serializers)
                Serialize(serializer, persistentDataEncoder);

            persistentDataEncoder.EndWritingBlock();
        }

        private void Serialize(
            IPersistentDataSerializer serializer,
            IWritablePersistentDataEncoder persistentDataEncoder
        )
        {
            persistentDataEncoder.BeginWritingBlock(serializer.Key());
            serializer.SerializeTo(persistentDataEncoder);
            persistentDataEncoder.EndWritingBlock();
        }

        public void LoadAll()
        {
            foreach (var channel in _channelSerializers.Keys)
                Load(channel);
        }

        public void Load(IChannel channel)
        {
            if (ChannelDoesNotExists(channel))
                throw new PersistentDataChannelNotFoundException(channel);

            if (_persistentDataIOStreamFactory.HasReadableStreamFor(channel) == false)
                return;

            using (var readStream = _persistentDataIOStreamFactory.CreateReadStreamFor(channel))
            {
                _persistentDataEncoder.Clear();
                _persistentDataEncoder.ReadFrom(readStream);

                _persistentDataEncoder.BeginReadingBlock("Data");

                foreach (var serializer in _channelSerializers[channel].Items())
                    TryDeserialize(serializer, _persistentDataEncoder);

                _persistentDataEncoder.EndReadingBlock();
            }
        }

        public void Delete(IChannel channel)
        {
            _persistentDataIOStreamFactory.Delete(channel);
        }

        private void TryDeserialize(
            IPersistentDataSerializer serializer,
            IReadablePersistentDataEncoder persistentDataEncoder
        )
        {
            if (persistentDataEncoder.HasKey(serializer.Key()))
            {
                persistentDataEncoder.BeginReadingBlock(serializer.Key());
                serializer.DeserializeFrom(persistentDataEncoder);
                persistentDataEncoder.EndReadingBlock();
            }
        }

        public bool Contains(IPersistentDataSerializer serializer)
        {
            return _allSerializers.Contains(serializer);
        }

        private bool IsSerializerRegistered(IPersistentDataSerializer serializer)
        {
            return _allSerializers.Contains(serializer);
        }

        private bool ChannelDoesNotExists(IChannel channel)
        {
            return _channelSerializers.ContainsKey(channel) == false;
        }

        private void TryCreateChannelData(IChannel channel)
        {
            if (ChannelDoesNotExists(channel))
                _channelSerializers.Add(channel, new Container<IPersistentDataSerializer>());
        }
    }
}
