using System;
using System.Collections.Generic;

namespace Arman.HttpConnection
{
    public enum HttpRequestType
    {
        GET,
        POST,
    }

    struct RequestSession
    {
        public readonly HttpRequest Request;
        public readonly Action<string> OnSuccess;
        public readonly Action<string> OnFailure;

        public RequestSession(
            HttpRequest request,
            Action<string> onSuccess,
            Action<string> onFailure
        )
        {
            this.Request = request;
            this.OnSuccess = onSuccess;
            this.OnFailure = onFailure;
        }
    }

    public struct HttpRequest
    {
        public readonly HttpRequestType RequestType;
        public readonly string Url;
        public readonly string Body;
        public readonly Dictionary<string, string> Headers;
        public readonly Dictionary<string, string> Parameters;
        public readonly float? TimeOut;

        public HttpRequest(
            HttpRequestType requestType,
            string url,
            string body,
            Dictionary<string, string> headers,
            Dictionary<string, string> parameters,
            float? timeOut
        )
        {
            this.RequestType = requestType;
            this.Url = url;
            this.Body = body;
            this.Headers = headers;
            this.Parameters = parameters;
            this.TimeOut = timeOut;
        }
    }
}
