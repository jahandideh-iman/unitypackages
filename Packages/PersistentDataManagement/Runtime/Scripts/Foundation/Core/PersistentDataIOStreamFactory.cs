using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataIOStreamFactory
    {
        bool HasReadableStreamFor(Channel channel);

        StreamWriter CreateWriteStreamFor(Channel channel);
        StreamReader CreateReadStreamFor(Channel channel);
    }
}