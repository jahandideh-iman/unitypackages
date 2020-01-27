using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class FileBasedPersistetDataIOStreamFactory : PersistentDataIOStreamFactory
    {
        public string relativePath;

        public FileBasedPersistetDataIOStreamFactory(string relativePath)
        {
            this.relativePath = relativePath;
        }

        public StreamReader CreateReadStreamFor(Channel channel)
        {
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Open);
            return new StreamReader(fs);
        }

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Create);
            return new StreamWriter(fs);
        }

        private string FilePathFor(Channel channel)
        {
            return Path.Combine(relativePath, channel.ToString());
        }
    }


}