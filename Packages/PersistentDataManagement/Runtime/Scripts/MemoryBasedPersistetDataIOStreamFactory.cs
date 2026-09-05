using Arman.PackageBasics;
using System.Collections.Generic;
using System.IO;

namespace Arman.PersistentDataManagement
{
    public class MemoryBasedPersistetDataIOStreamFactory : IPersistentDataIOStreamFactory
    {
        Dictionary<IChannel, MemoryStream> memoryStreams = new Dictionary<IChannel, MemoryStream>();

        public StreamReader CreateReadStreamFor(IChannel channel)
        {
            MemoryStream? memoryStream = null;

            memoryStreams.TryGetValue(channel, out memoryStream);
            if (memoryStream == null)
                memoryStream = new MemoryStream();


            return new StreamReader(new MemoryStream(memoryStream.ToArray()));
        }

        public StreamWriter CreateWriteStreamFor(IChannel channel)
        {
            var memoryStream = new MemoryStream();
            memoryStreams[channel] = memoryStream;
            return new StreamWriter(memoryStream);
        }

        public bool HasReadableStreamFor(IChannel channel)
        {
            return true;
        }

        public void Delete(IChannel channel)
        {
            memoryStreams.Remove(channel);
        }
    }
}