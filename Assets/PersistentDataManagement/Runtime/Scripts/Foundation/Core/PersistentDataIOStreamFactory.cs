using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataIOStreamFactory
    {
        StreamWriter CreateWriteStreamFor(Channel channel);
    }
}