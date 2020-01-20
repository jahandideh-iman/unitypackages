namespace Arman.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataSerializerAlreadyRegisterException : PersistentDataManagerException
    {
        public PersistentDataSerializer serializer;

        public PersistentDataSerializerAlreadyRegisterException(PersistentDataSerializer serializer)
        {
            this.serializer = serializer;
        }

        public override string ToString()
        {
            return $"Serializer \"{serializer}\" is already registered";
        }
    }

}