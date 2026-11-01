using System.Collections.Generic;

namespace Arman.HttpConnection
{
    public class HttpRequestBuilder
    {
        private HttpRequestType _requestType;
        private string _url;
        private string _body;
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private float? _timeout = null;

        public HttpRequestBuilder SetType(HttpRequestType type)
        {
            this._requestType = type;
            return this;
        }

        public HttpRequestBuilder SetURL(string url)
        {
            this._url = url;
            return this;
        }

        public HttpRequestBuilder SetBody(string body)
        {
            this._body = body;
            return this;
        }

        public HttpRequestBuilder AddHeader(string key, string value)
        {
            _headers[key] = value;
            return this;
        }

        public HttpRequestBuilder AddParameter(string key, string value)
        {
            _parameters[key] = value;
            return this;
        }

        public HttpRequestBuilder SetTimeout(float timeout)
        {
            this._timeout = timeout;
            return this;
        }

        public HttpRequest Build()
        {
            return new HttpRequest(_requestType, _url, _body, _headers, _parameters, _timeout);
        }
    }
}
