using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
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