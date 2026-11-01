using System.Collections.Generic;
using System.IO;
using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    public class MemoryBasedPersistentDataIOStreamFactory : IPersistentDataIOStreamFactory
    {
        Dictionary<IChannel, MemoryStream> _memoryStreams =
            new Dictionary<IChannel, MemoryStream>();

        public StreamReader CreateReadStreamFor(IChannel channel)
        {
            MemoryStream? memoryStream = null;

            _memoryStreams.TryGetValue(channel, out memoryStream);
            if (memoryStream == null)
                memoryStream = new MemoryStream();

            return new StreamReader(new MemoryStream(memoryStream.ToArray()));
        }

        public StreamWriter CreateWriteStreamFor(IChannel channel)
        {
            var memoryStream = new MemoryStream();
            _memoryStreams[channel] = memoryStream;
            return new StreamWriter(memoryStream);
        }

        public bool HasReadableStreamFor(IChannel channel)
        {
            return true;
        }

        public void Delete(IChannel channel)
        {
            _memoryStreams.Remove(channel);
        }
    }
}
