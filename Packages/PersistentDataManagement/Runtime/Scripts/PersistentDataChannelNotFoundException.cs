using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    public class PersistentDataChannelNotFoundException : PersistentDataManagerException
    {
        public IChannel channel;

        public PersistentDataChannelNotFoundException(IChannel channel)
        {
            this.channel = channel;
        }

        public override string ToString()
        {
            return $"Counldn't found channel : \"{channel}\"";
        }
    }

}