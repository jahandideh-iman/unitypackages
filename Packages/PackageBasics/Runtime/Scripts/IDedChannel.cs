using System.Collections.Generic;

namespace Arman.PackageBasics
{
    public class IDedChannel : IChannel
    {
        public readonly int Id;

        public IDedChannel(int id)
        {
            this.Id = id;
        }

        // TODO: Refactor this.
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            IDedChannel other = obj as IDedChannel;
            if ((System.Object)other == null)
            {
                return false;
            }

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return 1877310944 + Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
