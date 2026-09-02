using Arman.PackageBasics;
using System.IO;

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