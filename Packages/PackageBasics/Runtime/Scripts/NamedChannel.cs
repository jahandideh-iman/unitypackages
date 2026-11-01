using System.Collections.Generic;

namespace Arman.PackageBasics
{
    public class NamedChannel : IChannel
    {
        readonly string _name;

        public NamedChannel(string name)
        {
            this._name = name;
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

            return _name.Equals(other._name);
        }

        public override int GetHashCode()
        {
            return 1877310944 + EqualityComparer<string>.Default.GetHashCode(_name);
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
