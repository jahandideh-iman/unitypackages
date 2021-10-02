using System.Collections.Generic;

namespace Arman.HttpConnection
{
    public class HttpRequestBuilder
    {
        private HttpRequestType requestType;
        private string url;
        private string body;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private Dictionary<string, string> parameters = new Dictionary<string, string>();
        private float? timeout = null;

        public HttpRequestBuilder SetType(HttpRequestType type)
        {
            this.requestType = type;
            return this;
        }

        public HttpRequestBuilder SetURL(string url)
        {
            this.url = url;
            return this;
        }

        public HttpRequestBuilder SetBody(string body)
        {
            this.body = body;
            return this;
        }

        public HttpRequestBuilder AddHeader(string key, string value)
        {
            headers[key] = value;
            return this;
        }


        public HttpRequestBuilder AddParameter(string key, string value)
        {
            parameters[key] = value;
            return this;
        }

        public HttpRequestBuilder SetTimeout(float timeout)
        {
            this.timeout = timeout;
            return this;
        }

        public HttpRequest Build()
        {
            return new HttpRequest(requestType, url, body, headers, parameters, timeout);
        }


    }
}
