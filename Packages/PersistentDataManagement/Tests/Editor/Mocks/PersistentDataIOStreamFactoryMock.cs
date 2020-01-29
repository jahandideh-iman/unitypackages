
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using System.Collections.Generic;
using System.IO;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataIOStreamFactoryMock : PersistentDataIOStreamFactory
    {
        Dictionary<Channel, int> createWriteStreamCounts = new Dictionary<Channel, int>();
        Dictionary<Channel, int> createReadStreamCounts = new Dictionary<Channel, int>();


        public bool HasReadableStreamFor(Channel channel)
        {
            return true;
        }

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            if (createWriteStreamCounts.ContainsKey(channel) == false)
                createWriteStreamCounts.Add(channel, 0);
            createWriteStreamCounts[channel]++;

            return null;
        }


        public StreamReader CreateReadStreamFor(Channel channel)
        {
            if (createReadStreamCounts.ContainsKey(channel) == false)
                createReadStreamCounts.Add(channel, 0);
            createReadStreamCounts[channel]++;

            return null;
        }

        public bool CreateWriteStreamIsCalledOnceFor(Channel channel)
        {
            return createWriteStreamCounts.ContainsKey(channel) && createWriteStreamCounts[channel] == 1;
        }

        public bool CreateReadStreamIsCalledOnceFor(Channel channel)
        {
            return createReadStreamCounts.ContainsKey(channel) && createReadStreamCounts[channel] == 1;
        }

    }
}