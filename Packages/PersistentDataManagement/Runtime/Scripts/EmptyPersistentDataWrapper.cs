using System.IO;

namespace Arman.PersistentDataManagement
{
    public class EmptyPersistentDataWrapper : IPersistentDataWrapper
    {
        public void Clear() { }

        public void WriteTo(StreamWriter stream) { }

        public void ReadFrom(StreamReader stream) { }

        public bool HasKey(string key) { return false; }

        public int ReadInt(string key, int defaultValue = 0 ) { return 0; }

        public string ReadString(string key, string defaultValue = "") { return ""; }

        public float ReadFloat(string key, float defaultValue = 0f) { return 0f; }

        public bool ReadBoolean(string key, bool defaultValue = false) { return false; }


        public IWritablePersistentDataWrapper WriteInt(string key, int value) { return this; }

        public IWritablePersistentDataWrapper WriteString(string key, string value) { return this; }

        public IWritablePersistentDataWrapper WriteFloat(string key, float value) { return this; }

        public IWritablePersistentDataWrapper WriteBoolean(string key, bool value) { return this; }

        public void BeginReadingBlock(string key) { }

        public void EndReadingBlock() { }

        public IWritablePersistentDataWrapper BeginWritingBlock(string key) { return this; }
        public IWritablePersistentDataWrapper EndWritingBlock() { return this; }


    }

}