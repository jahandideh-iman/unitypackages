using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class EmptyPersistetDataIOStreamFactory : PersistentDataIOStreamFactory
    {
        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            return new StreamWriter(new MemoryStream());
        }
    }


}