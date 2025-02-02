using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Arman.HttpConnection
{
    public class UnityWebRequestBasedHttpConnectionService : MonoBehaviour, HttpConnectionService
    {
        // A temporary way to detecting timeout error by callers.
        // TODO: Find a better way.
        const string TIME_OUT_ERROR_MESSAGE = "Timeout";

        float defaultTimeout = 5f;


        public void SetDefaultTimeOut(float timeOut)
        {
            this.defaultTimeout = timeOut;
        }

        public void Request(HttpRequest request, Action<string> onSuccess, Action<string> onFailure)
        {
            var webRequest = CreateWebRequestFrom(request);
            webRequest.timeout = (int)(request.timeOut != null ? request.timeOut : defaultTimeout);
            Debug.Log($"Requesting {request.requestType}:{webRequest.url}");
            StartCoroutine(Connecting(webRequest, onSuccess, onFailure));
        }

        private UnityWebRequest CreateWebRequestFrom(HttpRequest request)
        {
            UnityWebRequest webRequest;

            switch (request.requestType)
            {
                case HttpRequestType.POST:
                    var postData = Encoding.UTF8.GetBytes(request.body);
                    // Note: It seems giving request.body directly to UnityWebRequest doesn't work.
                    webRequest = UnityWebRequest.PostWwwForm(request.url, "fake");
                    webRequest.uploadHandler = new UploadHandlerRaw(postData);
                    AddHeaders(webRequest, request.headers);
                    return webRequest;

                case HttpRequestType.GET:
                    var parameters = FormatedParameters(request.parameters);
                    webRequest = UnityWebRequest.Get(request.url + parameters);
                    AddHeaders(webRequest, request.headers);
                    return webRequest;
            }

            return null;
        }

        private void AddHeaders(UnityWebRequest unityWebRequest, Dictionary<string, string> headers)
        {
            foreach (var element in headers)
                unityWebRequest.SetRequestHeader(element.Key, element.Value);
        }

        private string FormatedParameters(Dictionary<string, string> parameters)
        {
            if (parameters.Count == 0)
                return "";
            var resultBuilder = new StringBuilder("?");

            foreach (var pair in parameters)
            {
                resultBuilder.Append(pair.Key);
                resultBuilder.Append("=");
                resultBuilder.Append(pair.Value);
                resultBuilder.Append("&");
            }

            // NOTE: Removing the last '&'.
            // WARNING: This implementation may be dangerous.
            resultBuilder.Length--;

            return resultBuilder.ToString();
        }


        // TODO: Refactor this.
        // TODO: Try to Remove/Disable the logs
        private IEnumerator Connecting(UnityWebRequest request, Action<string> successCallback, Action<string> failCallback)
        {
            yield return request.SendWebRequest();

            var response = request.downloadHandler.text;

            if (request.isNetworkError || request.isHttpError)
            {
                // TODO: Find a better way to determine timeout.
                if ("Request timeout".Equals(request.error))
                    failCallback(TIME_OUT_ERROR_MESSAGE);
                else
                    failCallback(response);
            }
            else
                successCallback(response);
        }

        public bool IsTimeOut(string msg)
        {
            return TIME_OUT_ERROR_MESSAGE.Equals(msg);
        }
    }
}