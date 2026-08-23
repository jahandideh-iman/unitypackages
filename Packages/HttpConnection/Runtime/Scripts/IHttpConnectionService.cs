using Arman.Foundation.Core.ServiceLocating;
using System;

namespace Arman.HttpConnection
{
    public interface IHttpConnectionService : IService
    {
        void Request(HttpRequest request, Action<string> onSuccess, Action<string> onFailure);

        bool IsTimeOut(string msg);
    }


}