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

        int ReadInt(string key);
        float ReadtFloat(string key);
        bool ReadBoolean(string key);
        string ReadString(string key);

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