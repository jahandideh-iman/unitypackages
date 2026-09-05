# Package Basics

The foundation the other Arman packages build on: a generic typed container, a channel identity
abstraction, and a bundled JSON implementation. Pure C# — it does not reference `UnityEngine`, so it
is testable as a plain library.

`Persistent Data Management` and `Update Management` both depend on this package, principally for
`IChannel`.

## What it provides

Namespace `Arman.Utility.Core`:

| Type | Purpose |
|---|---|
| `IContainer<T>` | `Add`, `Contains`, `Find<U>`, `FindAll<U>`, `Items` — a heterogeneous bag queried by subtype. |
| `Container<T>` | The in-memory implementation. |
| `IChannel` | An identity used to partition work into named groups. Requires value `Equals` / `GetHashCode`. |
| `NamedChannel` | An `IChannel` identified by a string. |
| `IDedChannel` | An `IChannel` identified by an integer. |

Namespace `NiceJson` — a bundled third-party JSON parser and serializer (`JsonNode`, `JsonObject`,
`JsonArray`, `JsonBasic`). MIT licensed; see `Third Party Notices.md`.

## Usage

`IContainer<T>` holds items of a common base type and retrieves them by their concrete subtype:

```csharp
using Arman.Utility.Core;

var systems = new Container<IGameSystem>();
systems.Add(new AudioSystem());
systems.Add(new SaveSystem());

AudioSystem audio = systems.Find<AudioSystem>();
ICollection<IGameSystem> all = systems.FindAll<IGameSystem>();

bool present = systems.Contains(audio);
foreach (IGameSystem system in systems.Items()) { /* ... */ }
```

Channels are value-equal identities, so they work as dictionary keys and can be recreated anywhere
from the same name:

```csharp
IChannel playerData = new NamedChannel("player");
IChannel worldData  = new NamedChannel("world");

// Equal by value, not reference — this is the point.
new NamedChannel("player").Equals(playerData); // true

IChannel slot = new IDedChannel(3);
```

Downstream packages take a channel to scope an operation:

```csharp
persistentDataManager.Register(playerSerializer, playerData);
persistentDataManager.Save(playerData);      // writes only the player channel

updateManager.RegisterUpdatable(enemy, gameplayChannel);
updateManager.Pause(gameplayChannel);        // freezes gameplay, not UI
```

Parsing JSON with the bundled `NiceJson`:

```csharp
using NiceJson;

JsonNode root = JsonNode.ParseJsonString("{\"score\":42}");
int score = root["score"];

var payload = new JsonObject();
payload["name"] = "player";
string json = payload.ToJsonString();
```

## Things to know

- **`IChannel` is an identity, not a container.** It carries no payload; it exists so other packages
  can group registrations under a stable, comparable key.
- **`NamedChannel` and `IDedChannel` compare by value but not with each other.** `NamedChannel`'s
  `Equals` returns `false` for anything that is not a `NamedChannel`, so a name and an id never
  collide.
- **`Find<U>()` returns the first assignable match** by linear scan — appropriate at the
  handful-of-items scale this is built for. With no match it returns `null`, so `U` should be a
  reference type; a value-type `U` throws on the cast instead.
- **`Container<T>` does not deduplicate.** `Add` appends unconditionally, and `Find<U>()`
  returns whichever matching item was added first.
- **`NiceJson` is vendored third-party code** under `Runtime/ThirdParties/`. Keep it in its own
  namespace and leave it unmodified so it stays upgradable.
