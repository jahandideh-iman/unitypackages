using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class EmptyPersistentDataWrapper : PersistentDataWrapper
    {
        public void Clear() { }

        public void WriteTo(StreamWriter stream) { }

        public void ReadFrom(StreamReader stream) { }
        public int LoadInt(string key) { return 0; }

        public string LoadString(string key) { return ""; }


        public void SaveInt(string key, int value) { }

        public void SaveString(string key, string value) { }

    }

}