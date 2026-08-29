# Package Basics

The foundational building blocks for the other Arman packages: a generic typed `IContainer` with *channels* that support optional dedup (`IDedChannel`), plus a bundled `NiceJson` implementation for JSON serialization. Other packages (e.g. Persistent Data Management, Update Management) depend on this package.

## What it provides

- `IContainer` / `BasicContainer` — a named container holding channels.
- `IChannel<T>` / `IDedChannel<T>` / `NamedChannel<T>` — typed push/pop channels.
- `NiceJson` — a lightweight JSON serializer (bundled in `ThirdParties/`).

## Usage

```csharp
using Arman.PackageBasics;

BasicContainer container = new BasicContainer("game");
IChannel<string> messages = new NamedChannel<string>(container, "messages");

messages.Push("hello");
string value = messages.Pop();
```
