using System.IO;

namespace Arman.PersistentDataManagement
{
    public interface IPersistentDataEncoder
        : IReadablePersistentDataEncoder,
            IWritablePersistentDataEncoder
    {
        void Clear();

        void WriteTo(StreamWriter stream);
        void ReadFrom(StreamReader stream);
    }

    public interface IReadablePersistentDataEncoder
    {
        bool HasKey(string key);

        int ReadInt(string key, int defaultValue = 0);
        float ReadFloat(string key, float defaultValue = 0f);
        bool ReadBoolean(string key, bool defaultValue = false);
        string ReadString(string key, string defaultValue = "");

        void BeginReadingBlock(string key);
        void EndReadingBlock();
    }

    public interface IWritablePersistentDataEncoder
    {
        IWritablePersistentDataEncoder WriteInt(string key, int value);
        IWritablePersistentDataEncoder WriteFloat(string key, float value);
        IWritablePersistentDataEncoder WriteBoolean(string key, bool value);
        IWritablePersistentDataEncoder WriteString(string key, string value);

        IWritablePersistentDataEncoder BeginWritingBlock(string key);
        IWritablePersistentDataEncoder EndWritingBlock();
    }
}
