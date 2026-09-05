using Arman.PackageBasics;

namespace Arman.PersistentDataManagement
{
    public interface IPersistentDataManager
    {
        void Register(IPersistentDataSerializer serializer);
        void Register(IPersistentDataSerializer serializer, IChannel channel);

        bool Contains(IPersistentDataSerializer serializer);

        void SaveAll();
        void Save(IChannel channel);

        void LoadAll();
        void Load(IChannel channel);
        void Delete(IChannel channel);
    }

}