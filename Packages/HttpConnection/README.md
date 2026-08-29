# Http Connection

A thin wrapper over `UnityWebRequest` for issuing HTTP requests. Build a request with a fluent `HttpRequestBuilder` (method, headers, body) and send it through an `IHttpConnectionService` that calls back with the success flag and response body.

## What it provides

- `HttpRequest` / `HttpRequestBuilder` — request model and fluent construction.
- `IHttpConnectionService` — the service contract (sync and async `MakeRequest` overloads).
- `UnityWebRequestBasedHttpConnectionService` — the concrete Unity implementation.

## Usage

```csharp
using Arman.HttpConnection;

IHttpConnectionService service = new UnityWebRequestBasedHttpConnectionService();

HttpRequest request = new HttpRequestBuilder("https://api.example.com/data")
    .AddHeader("Accept", "application/json")
    .AddBody("{\"key\":\"value\"}")
    .CreateRequest();

service.MakeRequest(request, (success, body) => Debug.Log($"Success={success}: {body}"));
```
