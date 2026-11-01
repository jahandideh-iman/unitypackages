using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    public class PersistentDataChannelNotFoundException : PersistentDataManagerException
    {
        public IChannel Channel;

        public PersistentDataChannelNotFoundException(IChannel channel)
        {
            this.Channel = channel;
        }

        public override string ToString()
        {
            return $"Counldn't found channel : \"{Channel}\"";
        }
    }
}
