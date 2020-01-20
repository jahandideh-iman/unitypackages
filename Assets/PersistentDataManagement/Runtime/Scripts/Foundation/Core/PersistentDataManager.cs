using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataManager
    {
        void SetPersistentDataHandler(PersistentDataWrapper handler);

        void Register(PersistentDataSerializer serializer);
        void Register(PersistentDataSerializer serializer, Channel channel);

        bool Contains(PersistentDataSerializer serializer);

        void Save(Channel channel);
        void SaveAll();
    }

}