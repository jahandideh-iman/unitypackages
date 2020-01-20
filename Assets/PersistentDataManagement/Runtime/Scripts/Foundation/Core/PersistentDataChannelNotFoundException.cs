using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataChannelNotFoundException : PersistentDataManagerException
    {
        public Channel channel;

        public PersistentDataChannelNotFoundException(Channel channel)
        {
            this.channel = channel;
        }

        public override string ToString()
        {
            return $"Counldn't found channel : \"{channel}\"";
        }
    }

}