using System;

namespace Arman.HttpConnection
{
    public interface IHttpConnectionService
    {
        void Request(HttpRequest request, Action<string> onSuccess, Action<string> onFailure);

        bool IsTimeOut(string msg);
    }


}