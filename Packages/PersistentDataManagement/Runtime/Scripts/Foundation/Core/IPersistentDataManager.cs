using Arman.Utility.Core;

namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface IPersistentDataManager
    {
        void SetSaveVersion(int version);
        void SetPersistentDataWrapper(IPersistentDataWrapper wrapper);
        void SetPersistentDataIOStreamFactory(IPersistentDataIOStreamFactory factory);

        void Register(IPersistentDataSerializer serializer);
        void Register(IPersistentDataSerializer serializer, IChannel channel);

        bool Contains(IPersistentDataSerializer serializer);

        void SaveAll();
        void Save(IChannel channel);

        void LoadAll();
        void Load(IChannel channel);
    }

}