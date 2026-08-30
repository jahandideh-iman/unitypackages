# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-30

First release of *Http Connection*.

### Added

- The `HttpRequest` struct and `HttpRequestType` enum.
- `HttpRequestBuilder`, a fluent builder with `SetType`, `SetURL`, `SetBody`, `AddHeader`, `AddParameter`, `SetTimeout` and `Build`.
- `IHttpConnectionService` and `UnityWebRequestBasedHttpConnectionService`, a `MonoBehaviour` that issues requests through `UnityWebRequest` with `onSuccess` / `onFailure` callbacks, a configurable default timeout, and `IsTimeOut` for classifying failures.
