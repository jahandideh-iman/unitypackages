using Arman.Foundation.Core.ServiceLocating;

namespace Arman.Foundation.InGameMessageLogging
{
    public interface IInGameMessageLogger : IService
    {
        void Log(string message);
    }
}