using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataWrapper : ReadablePersistentDataWrapper, WritablePersistentDataWrapper
    {
        void Clear();

        void WriteTo(StreamWriter stream);
        void ReadFrom(StreamReader stream);
    }

    public interface ReadablePersistentDataWrapper
    {
        bool HasKey(string key);

        int ReadInt(string key, int defaultValue = 0);
        float ReadFloat(string key, float defaultValue = 0f);
        bool ReadBoolean(string key, bool defaultValue = false);
        string ReadString(string key, string defaultValue = "");

        void BeginReadingBlock(string key);
        void EndReadingBlock();

    }
    public interface WritablePersistentDataWrapper
    {
        WritablePersistentDataWrapper WriteInt(string key, int value);
        WritablePersistentDataWrapper WriteFloat(string key, float value);
        WritablePersistentDataWrapper WriteBoolean(string key, bool value);
        WritablePersistentDataWrapper WriteString(string key, string value);

        WritablePersistentDataWrapper BeginWritingBlock(string key);
        WritablePersistentDataWrapper EndWritingBlock();

    }


}