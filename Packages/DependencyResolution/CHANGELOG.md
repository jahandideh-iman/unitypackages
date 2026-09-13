# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-09-13

First release of *Dependency Resolution*.

### Added

- `IDependencyResolver` and its reflection-based implementation `ReflectionBasedDependecyResolver`, with `RegisterType<T>`, `RegisterFactory<T>` and `RegisterInstance<T>`.
- `IResolutionEntry<T>.As<U>()`, exposing one registration under additional assignable types that all resolve to the same instance.
- `DepedencyResolverExtentsions`, typed `RegisterFactory` overloads for factories taking zero to nine dependencies.
- `Build()`, which orders registrations topologically, constructs each exactly once, and returns an `IRepository` with `Get<T>` and `TryGet<T>`.
- Fail-fast validation: unassignable `As<U>()` targets and factory return types are rejected at registration; missing, duplicate and circular dependencies are rejected at `Build()`.
- Exceptions thrown by constructors and factories propagate unwrapped instead of as `TargetInvocationException`.
