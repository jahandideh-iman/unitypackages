using System.IO;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataWrapper
    {
        void Clear();

        void WriteTo(StreamWriter stream);

        void SaveInt(string key, int value);
        void SaveString(string key, string value);


    }


}