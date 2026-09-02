namespace Arman.PersistentDataManagement
{
    public interface IPersistentDataSerializer
    {
        string Key();

        void SerializeTo(IWritablePersistentDataWrapper persistentDataWrapper);
        void DeserializeFrom(IReadablePersistentDataWrapper persistentDataWrapper);
    }

}