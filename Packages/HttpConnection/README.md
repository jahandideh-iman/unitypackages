# Http Connection

A thin wrapper over `UnityWebRequest`. Build a request with the fluent `HttpRequestBuilder` — method,
URL, headers, query parameters, body, timeout — and send it through an `IHttpConnectionService`,
which reports the result through separate success and failure callbacks.

## What it provides

Namespace `Arman.HttpConnection`:

| Type | Purpose |
|---|---|
| `HttpRequest` | Immutable request struct: `requestType`, `url`, `body`, `headers`, `parameters`, `timeOut`. |
| `HttpRequestType` | The HTTP method — `GET` or `POST`. |
| `HttpRequestBuilder` | `SetType`, `SetURL`, `SetBody`, `AddHeader`, `AddParameter`, `SetTimeout`, `Build`. |
| `IHttpConnectionService` | `Request(request, onSuccess, onFailure)` and `IsTimeOut(msg)`. |
| `UnityWebRequestBasedHttpConnectionService` | The `MonoBehaviour` implementation, plus `SetDefaultTimeOut`. |

## Usage

```csharp
using Arman.HttpConnection;
using UnityEngine;

// The service is a MonoBehaviour — get it from the scene, don't construct it.
IHttpConnectionService service = connectionServiceComponent;

HttpRequest request = new HttpRequestBuilder()
    .SetURL("https://api.example.com/data")
    .SetType(HttpRequestType.POST)
    .AddHeader("Accept", "application/json")
    .AddParameter("page", "1")
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

Setting a default timeout once, so individual requests don't have to:

```csharp
connectionServiceComponent.SetDefaultTimeOut(15f);
```

## Things to know

- **`UnityWebRequestBasedHttpConnectionService` is a `MonoBehaviour`.** It needs to live on a
  GameObject to run its coroutines — it cannot be constructed with `new`.
- **Failure is reported as a string**, not a status code. `IsTimeOut(msg)` exists precisely because
  the caller otherwise cannot tell a timeout from any other transport error.
- **`SetTimeout` on the builder overrides the service default** for that one request; leaving it
  unset falls back to whatever `SetDefaultTimeOut` was given.
- **There is no `Task`-returning overload.** The API is callback-based.
