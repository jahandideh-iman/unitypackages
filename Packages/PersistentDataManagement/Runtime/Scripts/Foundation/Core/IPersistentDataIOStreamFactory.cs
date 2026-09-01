using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface IPersistentDataIOStreamFactory
    {
        bool HasReadableStreamFor(IChannel channel);

        StreamWriter CreateWriteStreamFor(IChannel channel);
        StreamReader CreateReadStreamFor(IChannel channel);
        void Delete(IChannel channel);
    }
}