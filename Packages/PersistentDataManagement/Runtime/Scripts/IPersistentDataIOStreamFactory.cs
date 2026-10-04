using System.IO;
using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    public interface IPersistentDataIOStreamFactory
    {
        bool HasReadableStreamFor(IChannel channel);

        StreamWriter CreateWriteStreamFor(IChannel channel);
        StreamReader CreateReadStreamFor(IChannel channel);
        void Delete(IChannel channel);
    }
}
