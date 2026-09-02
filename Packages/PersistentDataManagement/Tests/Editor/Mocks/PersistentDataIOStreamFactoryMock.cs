
using Arman.PackageBasics;
using System.Collections.Generic;
using System.IO;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataIOStreamFactoryMock : IPersistentDataIOStreamFactory
    {
        Dictionary<IChannel, int> createWriteStreamCounts = new Dictionary<IChannel, int>();
        Dictionary<IChannel, int> createReadStreamCounts = new Dictionary<IChannel, int>();
        Dictionary<IChannel, int> deleteCallCounts = new Dictionary<IChannel, int>();


        public bool HasReadableStreamFor(IChannel channel)
        {
            return true;
        }

        public StreamWriter CreateWriteStreamFor(IChannel channel)
        {
            if (createWriteStreamCounts.ContainsKey(channel) == false)
                createWriteStreamCounts.Add(channel, 0);
            createWriteStreamCounts[channel]++;

            return null;
        }


        public StreamReader CreateReadStreamFor(IChannel channel)
        {
            if (createReadStreamCounts.ContainsKey(channel) == false)
                createReadStreamCounts.Add(channel, 0);
            createReadStreamCounts[channel]++;

            return null;
        }

        public void Delete(IChannel channel)
        {
            if (deleteCallCounts.ContainsKey(channel) == false)
                deleteCallCounts.Add(channel, 0);
            deleteCallCounts[channel]++;
        }

        public bool CreateWriteStreamIsCalledOnceFor(IChannel channel)
        {
            return createWriteStreamCounts.ContainsKey(channel) && createWriteStreamCounts[channel] == 1;
        }

        public bool CreateReadStreamIsCalledOnceFor(IChannel channel)
        {
            return createReadStreamCounts.ContainsKey(channel) && createReadStreamCounts[channel] == 1;
        }

        public bool DeleteIsCalledOnceFor(IChannel channel)
        {
            return deleteCallCounts.ContainsKey(channel) && deleteCallCounts[channel] == 1;
        }

    }
}