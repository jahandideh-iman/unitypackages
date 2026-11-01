# Http Connection

A thin wrapper over `UnityWebRequest`. Build a request with the fluent `HttpRequestBuilder` — method,
URL, headers, query parameters, body, timeout — and send it through an `IHttpConnectionService`,
which reports the result through separate success and failure callbacks.

## What it provides

Namespace `Arman.HttpConnection`:

| Type                                        | Purpose                                                                                     |
| ------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `HttpRequest`                               | Immutable request struct: `RequestType`, `Url`, `Body`, `Headers`, `Parameters`, `TimeOut`. |
| `HttpRequestType`                           | The HTTP method — `GET` or `POST`.                                                          |
| `HttpRequestBuilder`                        | `SetType`, `SetURL`, `SetBody`, `AddHeader`, `AddParameter`, `SetTimeout`, `Build`.         |
| `IHttpConnectionService`                    | `Request(request, onSuccess, onFailure)` and `IsTimeOut(msg)`.                              |
| `UnityWebRequestBasedHttpConnectionService` | The `MonoBehaviour` implementation, plus `SetDefaultTimeOut`.                               |

## Usage

```csharp
using Arman.HttpConnection;
using UnityEngine;

// The service is a MonoBehaviour — get it from the scene, don't construct it.
IHttpConnectionService service = connectionServiceComponent;

HttpRequest request = new HttpRequestBuilder()
    .SetURL("https://api.example.com/data")
    .SetType(HttpRequestType.POST)
    .AddHeader("Content-Type", "application/json")
    .AddHeader("Accept", "application/json")
    .SetBody("{\"key\":\"value\"}")
    .SetTimeout(10f)
    .Build();

service.Request(
    request,
    onSuccess: body => Debug.Log($"OK: {body}"),
    onFailure: error =>
    {
        if (service.IsTimeOut(error))
            Debug.LogWarning("Request timed out");
        else
            Debug.LogError($"Failed: {error}");
    });
```

Query parameters go on a `GET` request, where they are appended to the URL as `?page=1`:

```csharp
HttpRequest page = new HttpRequestBuilder()
    .SetURL("https://api.example.com/data")
    .SetType(HttpRequestType.GET)
    .AddParameter("page", "1")
    .Build();
```

Setting a default timeout once, so individual requests don't have to:

```csharp
connectionServiceComponent.SetDefaultTimeOut(15f);
```

## Things to know

- **`UnityWebRequestBasedHttpConnectionService` is a `MonoBehaviour`.** It needs to live on a
  GameObject to run its coroutines — it cannot be constructed with `new`.
- **Failure is reported as a string**, not a status code. On an HTTP or transport error the failure
  callback receives the response body, which is empty for a transport error. A timeout arrives as the
  fixed value `"Timeout"`, and `IsTimeOut(msg)` is the way to recognise it.
- **Parameters apply only to `GET`; the body applies only to `POST`.** A `GET` ignores `SetBody`, and a
  `POST` ignores `AddParameter`.
- **A `POST` body is sent as form data unless you set `Content-Type`.** The service starts from
  `UnityWebRequest.PostWwwForm`, which sets `application/x-www-form-urlencoded`; an `AddHeader` for
  `Content-Type` replaces it, as in the example above.
- **`SetTimeout` on the builder overrides the service default** for that one request; leaving it
  unset falls back to whatever `SetDefaultTimeOut` was given.
- **There is no `Task`-returning overload.** The API is callback-based.
