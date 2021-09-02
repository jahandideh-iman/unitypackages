using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class EmptyPersistentDataWrapper : PersistentDataWrapper
    {
        public void Clear() { }

        public void WriteTo(StreamWriter stream) { }

        public void ReadFrom(StreamReader stream) { }

        public bool HasKey(string key) { return false; }

        public int ReadInt(string key, int defaultValue = 0 ) { return 0; }

        public string ReadString(string key, string defaultValue = "") { return ""; }

        public float ReadFloat(string key, float defaultValue = 0f) { return 0f; }

        public bool ReadBoolean(string key, bool defaultValue = false) { return false; }


        public WritablePersistentDataWrapper WriteInt(string key, int value) { return this; }

        public WritablePersistentDataWrapper WriteString(string key, string value) { return this; }

        public WritablePersistentDataWrapper WriteFloat(string key, float value) { return this; }

        public WritablePersistentDataWrapper WriteBoolean(string key, bool value) { return this; }

        public void BeginReadingBlock(string key) { }

        public void EndReadingBlock() { }

        public WritablePersistentDataWrapper BeginWritingBlock(string key) { return this; }
        public WritablePersistentDataWrapper EndWritingBlock() { return this; }


    }

}