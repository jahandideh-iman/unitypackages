# Arman Unity Packages

A set of small, independent UPM packages for Unity — the reusable parts of several shipped games,
pulled out and published one package at a time so a project can take only what it needs.

Most are plain C# with no `UnityEngine` dependency in their core layer, with the Unity-facing pieces
kept in a separate namespace. Each package documents its own API in its `README.md`.

## Packages

| Package | Id | Purpose |
|---|---|---|
| [Asset Providing](Packages/Asset%20Providing) | `com.arman.asset-providing` | Load assets by id through sync or async providers. |
| [Component System](Packages/ComponentSystem) | `com.arman.component-system` | Composition over inheritance — entities as bags of components. |
| [Configuration Management](Packages/ConfigurationManagement) | `com.arman.configuration-management` | Apply configuration to objects by type, via registered configurers. |
| [Development Console](Packages/DevelopmentConsole) | `com.arman.development-console` | In-game cheat menu driven by a `[DevOption]` attribute. |
| [Event Management](Packages/EventManagement) | `com.arman.event-management` | A broadcast event bus for decoupling gameplay systems. |
| [Http Connection](Packages/HttpConnection) | `com.arman.http-connection` | A builder and service over `UnityWebRequest`. |
| [In Game Message Logging](Packages/InGameMessageLogging) | `com.arman.in-game-message-logging` | Capped, self-expiring on-screen log messages. |
| [Inventory System](Packages/InventorySystem) | `com.arman.inventory-system` | Generic quantity tracking with constraints. |
| [Object Pooling](Packages/ObjectPooling) | `com.arman.object-pooling` | Acquire/release pooling, with Unity component pools. |
| [Package Basics](Packages/PackageBasics) | `com.arman.package-basics` | Typed container, channel identities, bundled JSON. |
| [Persistent Data Management](Packages/PersistentDataManagement) | `com.arman.persistent-data-management` | Save/load split into serializer, encoding and storage. |
| [Scene Management](Packages/Scene%20Management) | `com.arman.scene-management` | Injectable scene loading and a per-scene initialiser. |
| [Service Locating](Packages/ServiceLocating) | `com.arman.service-locating` | A service locator for wiring systems together. |
| [Shop Management](Packages/ShopManagement) | `com.arman.shop-management` | Storefront and purchase routing, payment-agnostic. |
| [UI Management](Packages/UI%20Management) | `com.arman.ui-management` | A window stack for Unity UI, with popups and sorting. |
| [Unity Utilities](Packages/UnityUtilities) | `com.arman.unity-utilities` | Small Inspector-friendly `UnityEvent` helpers. |
| [Update Management](Packages/UpdateManagement) | `com.arman.update-management` | One update loop, with pausable nested channels. |

`Packages/PackageTemplate` is the scaffold for new packages. It is marked `private` and is never
published.

Only three packages depend on another: `in-game-message-logging` needs `unity-utilities`, and both
`persistent-data-management` and `update-management` need `package-basics`. Everything else is
standalone.

## Installing

Each package is published independently. Add it to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.arman.object-pooling": "0.1.0"
  }
}
```

Or install from the git URL, pinned to that package's tag:

```
https://github.com/jahandideh-iman/unitypackages.git?path=Packages/ObjectPooling#com.arman.object-pooling/0.1.0
```

The minimum Unity version is declared per package — 2019.1 for most, 2019.3 for a few.

## Repository layout

This is a Unity project that hosts the packages as embedded packages under `Packages/`. `Assets/` is
a scratch sandbox and is not part of any package.

Releases follow the OpenUPM model: a git tag `<package-id>/<version>` *is* the release. Tagging is
handled by `Tools/upm-release.mjs` (`validate`, `pack`, `tag`) and the `release` workflow.

```
node Tools/upm-release.mjs validate    # check every package
node Tools/upm-release.mjs pack        # npm pack --dry-run per package
```

## Contributing

Read [`.agents/AGENTS.md`](.agents/AGENTS.md) first. It covers the package anatomy, the asmdef and
`.meta` conventions, the release flow, and the deliberate inconsistencies that must be left alone.

## License

MIT — see [LICENSE](LICENSE). Packages that vendor third-party code carry their own
`Third Party Notices.md`.
