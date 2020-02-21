using Arman.Foundation.Core.ServiceLocating;

namespace Arman.Foundation.InGameMessageLogging
{
    public interface InGameMessageLogger : Service
    {
        void Log(string message);
    }
}