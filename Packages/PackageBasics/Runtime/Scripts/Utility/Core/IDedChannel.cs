using System.Collections.Generic;


namespace Arman.Utility.Core
{
    public class IDedChannel : Channel
    {
        public readonly int id;

        public IDedChannel(int id)
        {
            this.id = id;
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

            return id.Equals(other.id);
        }

        public override int GetHashCode()
        {
            return 1877310944 + id.GetHashCode();
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }
}