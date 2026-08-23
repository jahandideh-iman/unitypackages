using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface IPersistentDataWrapper : IReadablePersistentDataWrapper, IWritablePersistentDataWrapper
    {
        void Clear();

        void WriteTo(StreamWriter stream);
        void ReadFrom(StreamReader stream);
    }

    public interface IReadablePersistentDataWrapper
    {
        bool HasKey(string key);

        int ReadInt(string key, int defaultValue = 0);
        float ReadFloat(string key, float defaultValue = 0f);
        bool ReadBoolean(string key, bool defaultValue = false);
        string ReadString(string key, string defaultValue = "");

        void BeginReadingBlock(string key);
        void EndReadingBlock();

    }
    public interface IWritablePersistentDataWrapper
    {
        IWritablePersistentDataWrapper WriteInt(string key, int value);
        IWritablePersistentDataWrapper WriteFloat(string key, float value);
        IWritablePersistentDataWrapper WriteBoolean(string key, bool value);
        IWritablePersistentDataWrapper WriteString(string key, string value);

        IWritablePersistentDataWrapper BeginWritingBlock(string key);
        IWritablePersistentDataWrapper EndWritingBlock();

    }


}