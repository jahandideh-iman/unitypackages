
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using System.Collections.Generic;
using System.IO;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataIOStreamFactoryMock : PersistentDataIOStreamFactory
    {
        Dictionary<Channel, int> createCounts = new Dictionary<Channel, int>();

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            if (createCounts.ContainsKey(channel) == false)
                createCounts.Add(channel, 0);
            createCounts[channel]++;

            return null;
        }

        public bool CreateIsCalledOnceFor(Channel channel)
        {
            return createCounts.ContainsKey(channel) && createCounts[channel] == 1;
        }
    }
}