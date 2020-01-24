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
        int LoadInt(string key);
        string LoadString(string key);

    }
    public interface WritablePersistentDataWrapper
    {
        void SaveInt(string key, int value);
        void SaveString(string key, string value);

    }


}