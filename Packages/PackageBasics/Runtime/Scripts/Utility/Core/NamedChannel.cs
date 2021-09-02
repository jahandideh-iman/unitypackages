using System.Collections.Generic;

namespace Arman.Utility.Core
{
    public class NamedChannel : Channel
    {
        readonly string name;

        public NamedChannel(string name)
        {
            this.name = name;
        }


        // TODO: Refactor this.
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            NamedChannel other = obj as NamedChannel;
            if ((System.Object)other == null)
            {
                return false;
            }

            return name.Equals(other.name);
        }

        public override int GetHashCode()
        {
            return 1877310944 + EqualityComparer<string>.Default.GetHashCode(name);
        }

        public override string ToString()
        {
            return name;
        }
    }
}