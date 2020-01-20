namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataWrapper
    {
        void SaveInt(string key, int value);
        void SaveString(string key, string value);
    }

}