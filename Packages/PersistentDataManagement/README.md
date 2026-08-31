# Persistent Data Management

Saving and loading game state, split into three replaceable parts: *what* is saved
(`IPersistentDataSerializer`), *how it is encoded* (`IPersistentDataWrapper`), and *where it goes*
(`IPersistentDataIOStreamFactory`). Systems register a serializer and never learn whether their data
ends up in a JSON file, a memory stream or a test double.

Data is grouped by `IChannel`, so a save can be scoped: write the player's channel without touching
the world's, or load only settings on boot.

## What it provides

Namespace `Arman.Foundation.Core.PersistentDataManagement`:

| Type | Purpose |
|---|---|
| `IPersistentDataSerializer` | `Key()`, `SerializeTo(...)`, `DeserializeFrom(...)` — implemented by anything with state to save. |
| `IPersistentDataWrapper` | The encoding. Splits into `IWritablePersistentDataWrapper` and `IReadablePersistentDataWrapper`. |
| `IPersistentDataIOStreamFactory` | Supplies a `StreamReader`/`StreamWriter` per channel. |
| `IPersistentDataManager` / `BasicPersistentDataManager` | Registration, `Save`/`SaveAll`, `Load`/`LoadAll`, `SetSaveVersion`. |
| `MemoryBasedPersistetDataIOStreamFactory` | In-memory streams — for tests. |
| `EmptyPersistentDataWrapper`, `EmptyPersistetDataIOStreamFactory` | No-op stand-ins. |

Namespace `Arman.Foundation.Unity.PersistentDataManagement`:

| Type | Purpose |
|---|---|
| `JSONPersistentDataWrapper` | JSON encoding, via the `NiceJson` bundled in `Package Basics`. |
| `FileBasedPersistetDataIOStreamFactory` | One file per channel under a directory you pass in. |
| `PlayerPrefsPersistentDataWrapper` | `PlayerPrefs` backing — see the warning below. |

## Usage

Implement a serializer for each thing that has state. `Key()` names its block in the save:

```csharp
using Arman.Foundation.Core.PersistentDataManagement;

public class PlayerProgress : IPersistentDataSerializer
{
    private int _level;
    private float _health;
    private string _name;

    public string Key() => "PlayerProgress";

    public void SerializeTo(IWritablePersistentDataWrapper data)
    {
        data.WriteInt("level", _level)
            .WriteFloat("health", _health)
            .WriteString("name", _name);
    }

    public void DeserializeFrom(IReadablePersistentDataWrapper data)
    {
        _level  = data.ReadInt("level", 1);
        _health = data.ReadFloat("health", 100f);
        _name   = data.ReadString("name", "Player");
    }
}
```

Assemble a manager and register against channels:

```csharp
using Arman.Foundation.Unity.PersistentDataManagement;
using Arman.Utility.Core;
using UnityEngine;

var manager = new BasicPersistentDataManager(
    new FileBasedPersistetDataIOStreamFactory(Application.persistentDataPath),
    new JSONPersistentDataWrapper(),
    saveVersion: 1);

IChannel player   = new NamedChannel("player");
IChannel settings = new NamedChannel("settings");

manager.Register(new PlayerProgress(), player);
manager.Register(new AudioSettings(), settings);
```

Save and load, whole or by channel:

```csharp
manager.Save(player);      // writes only the player file
manager.SaveAll();

manager.Load(settings);    // no-op if nothing has been saved yet
manager.LoadAll();
```

Nested blocks let a serializer write structured data:

```csharp
public void SerializeTo(IWritablePersistentDataWrapper data)
{
    data.BeginWritingBlock("position")
        .WriteFloat("x", _x)
        .WriteFloat("y", _y)
        .EndWritingBlock();
}

public void DeserializeFrom(IReadablePersistentDataWrapper data)
{
    if (data.HasKey("position") == false)
        return;

    data.BeginReadingBlock("position");
    _x = data.ReadFloat("x");
    _y = data.ReadFloat("y");
    data.EndReadingBlock();
}
```

### Testing without touching the disk

Swap the stream factory; nothing else changes:

```csharp
var manager = new BasicPersistentDataManager(
    new MemoryBasedPersistetDataIOStreamFactory(),
    new JSONPersistentDataWrapper(),
    saveVersion: 1);
```

## Things to know

- **Do not use `PlayerPrefsPersistentDataWrapper`.** It is marked in-source as violating the
  `IPersistentDataWrapper` contract — `Clear`, `WriteTo` and `ReadFrom` are all no-ops, so it ignores
  the stream layer entirely. It ships for compatibility; prefer `JSONPersistentDataWrapper`.
- **The save file is named after the channel.** `FileBasedPersistetDataIOStreamFactory` uses
  `channel.ToString()` as the filename, so two channels whose `ToString()` matches overwrite each
  other. Give every `NamedChannel` a distinct name.
- **Registering the same serializer twice throws** `PersistentDataSerializerAlreadyRegisterException`.
- **Saving or loading an unknown channel throws** `PersistentDataChannelNotFoundException`. A channel
  exists once something has been registered on it.
- **Loading with no saved data is a silent no-op**, so a first run leaves your defaults in place —
  which is why `DeserializeFrom` should supply sensible fallbacks.
- **A serializer with no matching block in the file is skipped**, not reset. Adding a new serializer
  to an existing save works without a migration.
- **The save version is written but never verified on load.** `SetSaveVersion` records a `Version` in
  the metadata block for your own migration logic; the manager itself does not compare it.
- **`Register` with no channel uses an internal default channel**, whose file is named `_internal`.
