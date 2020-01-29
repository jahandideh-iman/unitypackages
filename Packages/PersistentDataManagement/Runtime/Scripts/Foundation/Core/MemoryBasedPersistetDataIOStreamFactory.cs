using Arman.Utility.Core;
using System.Collections.Generic;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class MemoryBasedPersistetDataIOStreamFactory : PersistentDataIOStreamFactory
    {
        Dictionary<Channel, MemoryStream> memoryStreams = new Dictionary<Channel, MemoryStream>();

        public StreamReader CreateReadStreamFor(Channel channel)
        {
            MemoryStream memoryStream = null;

            memoryStreams.TryGetValue(channel, out memoryStream);
            if (memoryStream == null)
                memoryStream = new MemoryStream();


            return new StreamReader(new MemoryStream(memoryStream.ToArray()));
        }

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            var memoryStream = new MemoryStream();
            memoryStreams[channel] = memoryStream;
            return new StreamWriter(memoryStream);
        }

        public bool HasReadableStreamFor(Channel channel)
        {
            return true;
        }
    }
}