namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataSerializerAlreadyRegisterException : PersistentDataManagerException
    {
        public IPersistentDataSerializer serializer;

        public PersistentDataSerializerAlreadyRegisterException(IPersistentDataSerializer serializer)
        {
            this.serializer = serializer;
        }

        public override string ToString()
        {
            return $"Serializer \"{serializer}\" is already registered";
        }
    }

}