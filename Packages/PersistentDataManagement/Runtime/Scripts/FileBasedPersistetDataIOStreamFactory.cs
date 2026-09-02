using Arman.PackageBasics;
using System.IO;

namespace Arman.PersistentDataManagement
{
    public class FileBasedPersistetDataIOStreamFactory : IPersistentDataIOStreamFactory
    {
        public string path;

        public FileBasedPersistetDataIOStreamFactory(string path)
        {
            this.path = path;
        }


        public bool HasReadableStreamFor(IChannel channel)
        {
            return File.Exists(FilePathFor(channel));
        }

        public StreamReader CreateReadStreamFor(IChannel channel)
        {
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Open);
            return new StreamReader(fs);
        }

        public StreamWriter CreateWriteStreamFor(IChannel channel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePathFor(channel)));
            FileStream fs = new FileStream(FilePathFor(channel), FileMode.Create);
            return new StreamWriter(fs);
        }

        public void Delete(IChannel channel)
        {
            var filePath = FilePathFor(channel);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private string FilePathFor(IChannel channel)
        {
            return Path.Combine(path, channel.ToString());
        }
    }


}