using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class EmptyPersistetDataIOStreamFactory : PersistentDataIOStreamFactory
    {
        public StreamReader CreateReadStreamFor(Channel channel)
        {
            return new StreamReader(new MemoryStream());
        }

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            return new StreamWriter(new MemoryStream());
        }

        public bool HasReadableStreamFor(Channel channel)
        {
            return true;
        }
    }
}