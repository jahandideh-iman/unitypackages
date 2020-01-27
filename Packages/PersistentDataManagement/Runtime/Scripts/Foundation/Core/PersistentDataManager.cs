using Arman.Foundation.Core.ServiceLocating;
using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataManager : Service
    {
        void SetSaveVersion(int version);
        void SetPersistentDataWrapper(PersistentDataWrapper wrapper);
        void SetPersistentDataIOStreamFactory(PersistentDataIOStreamFactory factory);

        void Register(PersistentDataSerializer serializer);
        void Register(PersistentDataSerializer serializer, Channel channel);

        bool Contains(PersistentDataSerializer serializer);

        void SaveAll();
        void Save(Channel channel);

        void LoadAll();
        void Load(Channel channel);
    }

}