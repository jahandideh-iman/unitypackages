using System;
using System.Collections.Generic;

namespace Arman.HttpConnection
{
    public enum HttpRequestType { GET, POST }

    
    struct RequestSession
    {
        public readonly HttpRequest request;
        public readonly Action<string> onSuccess;
        public readonly Action<string> onFailure;

        public RequestSession(HttpRequest request, Action<string> onSuccess, Action<string> onFailure)
        {
            this.request = request;
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }
    }
    
    
    public struct HttpRequest
    {
        public readonly HttpRequestType requestType;
        public readonly string url;
        public readonly string body;
        public readonly Dictionary<string, string> headers;
        public readonly Dictionary<string, string> parameters;
        public readonly float? timeOut;

        public HttpRequest(
            HttpRequestType requestType, 
            string url,
            string body,
            Dictionary<string, string> headers, 
            Dictionary<string, string> parameters,
            float? timeOut)
        {
            this.requestType = requestType;
            this.url = url;
            this.body = body;
            this.headers = headers;
            this.parameters = parameters;
            this.timeOut = timeOut;
        }
    }
}
