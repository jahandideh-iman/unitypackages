
namespace Arman.PackageBasics
{
    public interface IChannel
    {
        bool Equals(object obj);

        int GetHashCode();
    }
}