using Arman.Foundation.Core.ServiceLocating;
using System;

namespace Arman.HttpConnection
{
    public interface HttpConnectionService : Service
    {
        void Request(HttpRequest request, Action<string> onSuccess, Action<string> onFailure);

        bool IsTimeOut(string msg);
    }


}