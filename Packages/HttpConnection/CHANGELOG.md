# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-11-01

### Removed

- The camelCase names of public and protected fields, renamed to PascalCase: `RequestSession.request` → `RequestSession.Request`, `RequestSession.onSuccess` → `RequestSession.OnSuccess`, `RequestSession.onFailure` → `RequestSession.OnFailure`, `HttpRequest.requestType` → `HttpRequest.RequestType`, `HttpRequest.url` → `HttpRequest.Url`, `HttpRequest.body` → `HttpRequest.Body`, `HttpRequest.headers` → `HttpRequest.Headers`, `HttpRequest.parameters` → `HttpRequest.Parameters`, `HttpRequest.timeOut` → `HttpRequest.TimeOut`. **Breaking** — update references to the new names.

### Changed

- Renamed private fields that Unity does not serialize to `_camelCase`. No public API or serialized data changes.

## [0.2.0] - 2026-09-02

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

## [0.1.0] - 2026-08-30

First release of _Http Connection_.

### Added

- The `HttpRequest` struct and `HttpRequestType` enum.
- `HttpRequestBuilder`, a fluent builder with `SetType`, `SetURL`, `SetBody`, `AddHeader`, `AddParameter`, `SetTimeout` and `Build`.
- `IHttpConnectionService` and `UnityWebRequestBasedHttpConnectionService`, a `MonoBehaviour` that issues requests through `UnityWebRequest` with `onSuccess` / `onFailure` callbacks, a configurable default timeout, and `IsTimeOut` for classifying failures.
