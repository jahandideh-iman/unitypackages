# Persistent Data Management

Saving and loading game state, split into three replaceable parts: _what_ is saved
(`IPersistentDataSerializer`), _how it is encoded_ (`IPersistentDataEncoder`), and _where it goes_
(`IPersistentDataIOStreamFactory`). Systems register a serializer and never learn whether their data
ends up in a JSON file, a memory stream or a test double.

Data is grouped by `IChannel`, so a save can be scoped: write the player's channel without touching
the world's, or load only settings on boot.

## What it provides

Namespace `Arman.PersistentDataManagement`:

| Type                                                               | Purpose                                                                                           |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `IPersistentDataSerializer`                                        | `Key()`, `SerializeTo(...)`, `DeserializeFrom(...)` — implemented by anything with state to save. |
| `IPersistentDataEncoder`                                           | The encoding. Splits into `IWritablePersistentDataEncoder` and `IReadablePersistentDataEncoder`.  |
| `IPersistentDataIOStreamFactory`                                   | Supplies a `StreamReader`/`StreamWriter` per channel.                                             |
| `IPersistentDataManager` / `PersistentDataManager`                 | Registration, `Save`/`SaveAll`, `Load`/`LoadAll`, `Delete`.                                       |
| `MemoryBasedPersistentDataIOStreamFactory`                         | In-memory streams — for tests.                                                                    |
| `EmptyPersistentDataEncoder`, `EmptyPersistentDataIOStreamFactory` | No-op stand-ins.                                                                                  |

The same namespace holds the file, JSON and `PlayerPrefs` encoders and streams:

| Type                                     | Purpose                                                        |
| ---------------------------------------- | -------------------------------------------------------------- |
| `JSONPersistentDataEncoder`              | JSON encoding, via the `NiceJson` bundled in `Package Basics`. |
| `FileBasedPersistentDataIOStreamFactory` | One file per channel under a directory you pass in.            |
| `PlayerPrefsPersistentDataEncoder`       | `PlayerPrefs` backing — see the warning below.                 |

## Usage

Implement a serializer for each thing that has state. `Key()` names its block in the save:

```csharp
using Arman.PersistentDataManagement;

public class PlayerProgress : IPersistentDataSerializer
{
    private int _level;
    private float _health;
    private string _name;

    public string Key() => "PlayerProgress";

    public void SerializeTo(IWritablePersistentDataEncoder data)
    {
        data.WriteInt("level", _level)
            .WriteFloat("health", _health)
            .WriteString("name", _name);
    }

    public void DeserializeFrom(IReadablePersistentDataEncoder data)
    {
        _level  = data.ReadInt("level", 1);
        _health = data.ReadFloat("health", 100f);
        _name   = data.ReadString("name", "Player");
    }
}
```

Assemble a manager and register against channels:

```csharp
using Arman.PackageBasics;
using Arman.PersistentDataManagement;
using UnityEngine;

var manager = new PersistentDataManager(
    new FileBasedPersistentDataIOStreamFactory(Application.persistentDataPath),
    new JSONPersistentDataEncoder(),
    saveVersion: 1);

IChannel player   = new NamedChannel("player");
IChannel settings = new NamedChannel("settings");

manager.Register(new PlayerProgress(), player);
manager.Register(new AudioSettings(), settings);
```

`PersistentDataManagerFactory.CreateDefault(saveVersion)` builds the same JSON-file manager under
`Application.persistentDataPath`, for when the defaults are what you want.

Save and load, whole or by channel:

```csharp
manager.Save(player);      // writes only the player file
manager.SaveAll();

manager.Load(settings);    // no-op if nothing has been saved yet
manager.LoadAll();
```

Nested blocks let a serializer write structured data:

```csharp
public void SerializeTo(IWritablePersistentDataEncoder data)
{
    data.BeginWritingBlock("position")
        .WriteFloat("x", _x)
        .WriteFloat("y", _y)
        .EndWritingBlock();
}

public void DeserializeFrom(IReadablePersistentDataEncoder data)
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
var manager = new PersistentDataManager(
    new MemoryBasedPersistentDataIOStreamFactory(),
    new JSONPersistentDataEncoder(),
    saveVersion: 1);
```

## Things to know

- **Do not use `PlayerPrefsPersistentDataEncoder`.** It is marked in-source as violating the
  `IPersistentDataEncoder` contract — `Clear`, `WriteTo` and `ReadFrom` are all no-ops, so it ignores
  the stream layer entirely. It ships for compatibility; prefer `JSONPersistentDataEncoder`.
- **The save file is named after the channel.** `FileBasedPersistentDataIOStreamFactory` uses
  `channel.ToString()` as the filename, so two channels whose `ToString()` matches overwrite each
  other. Give every `NamedChannel` a distinct name.
- **Registering the same serializer twice throws** `PersistentDataSerializerAlreadyRegisterException`.
- **Saving or loading an unknown channel throws** `PersistentDataChannelNotFoundException`. A channel
  exists once something has been registered on it.
- **Loading with no saved data is a silent no-op**, so a first run leaves your defaults in place —
  which is why `DeserializeFrom` should supply sensible fallbacks.
- **A serializer with no matching block in the file is skipped**, not reset. Adding a new serializer
  to an existing save works without a migration.
- **An empty save file loads as "nothing saved yet".** `JSONPersistentDataEncoder` treats empty or
  whitespace-only content as absent, and a block key that is not present as absent rather than an error.
  A file cut off mid-write is not valid JSON, so `Load` throws while parsing it and applies none of
  its data.
- **The save version is written but never verified on load.** The `saveVersion` passed to the
  constructor records a `Version` in the metadata block for your own migration logic; the manager
  itself does not compare it.
- **`Register` with no channel uses an internal default channel**, whose file is named `_internal`.
