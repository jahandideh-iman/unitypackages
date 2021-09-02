using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using System.IO;

namespace Arman.Foundation.Unity.PersistentDataManagement
{
    public class FileBasedPersistetDataIOStreamFactory : PersistentDataIOStreamFactory
    {
        public string path;

        public FileBasedPersistetDataIOStreamFactory(string path)
        {
            this.path = path;
        }


        public bool HasReadableStreamFor(Channel channel)
        {
            return File.Exists(FilePathFor(channel));
        }

        public StreamReader CreateReadStreamFor(Channel channel)
        {
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Open);
            return new StreamReader(fs);
        }

        public StreamWriter CreateWriteStreamFor(Channel channel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePathFor(channel)));
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Create);
            return new StreamWriter(fs);
        }

        private string FilePathFor(Channel channel)
        {
            return Path.Combine(path, channel.ToString());
        }
    }


}