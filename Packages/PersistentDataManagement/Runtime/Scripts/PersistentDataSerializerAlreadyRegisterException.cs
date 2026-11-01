namespace Arman.PersistentDataManagement
{
    public class PersistentDataSerializerAlreadyRegisterException : PersistentDataManagerException
    {
        public IPersistentDataSerializer Serializer;

        public PersistentDataSerializerAlreadyRegisterException(
            IPersistentDataSerializer serializer
        )
        {
            this.Serializer = serializer;
        }

        public override string ToString()
        {
            return $"Serializer \"{Serializer}\" is already registered";
        }
    }
}
