using Arman.PackageBasics;
using System.IO;

namespace Arman.PersistentDataManagement
{
    public class EmptyPersistetDataIOStreamFactory : IPersistentDataIOStreamFactory
    {
        public StreamReader CreateReadStreamFor(IChannel channel)
        {
            return new StreamReader(new MemoryStream());
        }

        public StreamWriter CreateWriteStreamFor(IChannel channel)
        {
            return new StreamWriter(new MemoryStream());
        }

        public bool HasReadableStreamFor(IChannel channel)
        {
            return true;
        }

        public void Delete(IChannel channel)
        {
        }
    }
}