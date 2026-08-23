
namespace Arman.Utility.Core
{
    public interface IChannel
    {
        bool Equals(object obj);

        int GetHashCode();
    }
}