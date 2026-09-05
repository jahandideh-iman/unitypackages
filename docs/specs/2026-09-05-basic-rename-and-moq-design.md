# Dropping the `Basic` prefix, and Moq for the interaction tests

**Date:** 2026-09-05
**Status:** **Accepted.** Design approved 2026-09-05. Two independent parts, executed in order as two
branches off `dev`.

## 1. The problem

Two unrelated pieces of naming and testing debt, requested together.

**The `Basic` prefix carries no information.** Every package's principal implementation of its
principal interface is called `Basic<Something>` — `BasicUpdateManager` implements `IUpdateManager`,
`BasicShopCenter` implements `IShopCenter`, and so on for ten packages. In every case there is
exactly one implementation, so `Basic` does not distinguish it from anything; it just makes the type
name longer and the `I`-prefix convention inconsistent with itself. The natural name for the sole
implementation of `IUpdateManager` is `UpdateManager`.

**The hand-written test doubles are a mix of two different things wearing one name.** Fifteen classes
across the test assemblies are called `*Mock`, `Mock*`, or `Fake*` with no relation between the name
and what the class does. Some are genuine fakes — small working implementations that hold state and
are asserted on as values. Others are mocks in the strict sense: they exist only to count calls,
capture arguments, or record call ordering, and the test's assertion *is* the interaction. The second
group is hand-rolled infrastructure that a mocking library does better and in a tenth of the lines —
`PersistentDataWrapperMock` alone is 96 lines, roughly 60 of which are `NotImplementedException`
stubs for interface members no test touches.

The two parts are independent. They are specced together because the rename touches the same test
files the Moq conversion rewrites, so the order matters.

## 2. Scope

### 2.1 In scope

Twelve type renames, and the conversion of eight test doubles to Moq with five rewritten as
explicitly-named fakes.

### 2.2 Out of scope

* **Releasing.** Both parts land on `dev` with CHANGELOG entries under `## [Unreleased]`. Running
  `Tools/upm-release.mjs prepare`, promoting to `master`, and tagging stay a separate, deliberate
  human decision — see [Distribution and releases](../../.agents/AGENTS.md#distribution-and-releases).
* **Assembly names.** `Arman.X.Tests.Editor` vs `Arman.X.Editor.Tests` stays inconsistent. AGENTS.md
  is explicit that assembly renames break consumer asmdef references and should not be tidied
  opportunistically.
* **`Empty*` / `MemoryBased*` runtime helpers.** `EmptyPersistetDataIOStreamFactory` and
  `MemoryBasedPersistetDataIOStreamFactory` carry a typo ("Persistet") in a published API. Fixing it
  is a second breaking rename with no relation to this one; it is left alone deliberately.
* **`JsonBasic`** in `PackageBasics/Runtime/ThirdParties/NiceJson.cs` — vendored third-party code.
* **`FakeConfigurer<T>` and `FakeMultiConfigurerAB`** in ConfigurationManagement. They are already
  fakes and already named correctly.

## 3. Part A — dropping the `Basic` prefix

### 3.1 The renames

Ten of the twelve are the sole implementation of a matching interface. The remaining two are included
because leaving them would strand `Basic` in two public names for no reason.

| Package | From | To | Implements |
|---|---|---|---|
| `ComponentSystem` | `BasicEntity` | `Entity` | `IEntity` |
| `ComponentSystem` | `BasicSpecializedEntity<T>` | `SpecializedEntity<T>` | `ISpecializedEntity<T>` |
| `ComponentSystem` | `CacheableBasicEntity<T>` | `CacheableEntity<T>` | *derives from `BasicEntity`* |
| `ConfigurationManagement` | `BasicConfigurationManager` | `ConfigurationManager` | `IConfigurationManager` |
| `EventManagement` | `BasicEventManager` | `EventManager` | `IEventManager` |
| `InventorySystem` | `BasicInventory<T>` | `Inventory<T>` | `IInventory<T>` |
| `ObjectPooling` | `BasicObjectPool<T>` | `ObjectPool<T>` | `IObjectPool<T>` (abstract) |
| `PackageBasics` | `BasicContainer<T>` | `Container<T>` | `IContainer<T>` |
| `PersistentDataManagement` | `BasicPersistentDataManager` | `PersistentDataManager` | `IPersistentDataManager` |
| `ShopManagement` | `BasicShopCenter` | `ShopCenter` | `IShopCenter` |
| `UpdateManagement` | `BasicUpdateManager` | `UpdateManager` | `IUpdateManager` |
| `Asset Providing` | `BasicAssetProviderServiceConfig` | `AssetProviderServiceConfig` | *`ScriptableObject`* |

Test types follow their subject: `BasicUpdateManagerTest_ThrowingUpdatables` →
`UpdateManagerTest_ThrowingUpdatables`, `BasicPersistentDataManagerTestContext` →
`PersistentDataManagerTestContext`, `TestableBasicObjectPool` → `TestableObjectPool`, and so on.

### 3.2 How

Renames are driven by Roslyn (`sharplens rename_symbol`), not by text substitution, so generic
constraints, `using` aliases, and cross-package references come along correctly. AGENTS.md mandates
this for cross-package symbol renames.

Each `.cs` file is renamed to match its type, **and its `.meta` file is moved in the same commit**:

```
git mv Runtime/Scripts/BasicUpdateManager.cs      Runtime/Scripts/UpdateManager.cs
git mv Runtime/Scripts/BasicUpdateManager.cs.meta Runtime/Scripts/UpdateManager.cs.meta
```

This is load-bearing, not tidiness. A `.meta` carries the GUID that Unity binds assets to. Losing or
regenerating one silently rebinds — or unbinds — every asset that referenced the script.

### 3.3 Consequences

**This is a breaking change to published packages.** All 17 publishable packages are tagged at
`0.2.0`. Every rename removes a public type a consumer may reference. Per the decision recorded in
§6, no `[Obsolete]` forwarding shims are added: these are `0.x` packages where a breaking change is
expected, and a shim written to be deleted is work done twice.

Each of the ten affected packages gets a `### Removed` entry under `## [Unreleased]`, which under the
repo's derivation rules makes it a **minor** bump while the major is `0`. The bump itself happens
later, when `prepare` runs.

### 3.4 Risks

* **`ObjectPool<T>` shares a simple name with `UnityEngine.Pool.ObjectPool<T>`.** Both live in
  different namespaces, so this is only ambiguous for a consumer who imports `Arman.ObjectPooling`
  and `UnityEngine.Pool` in the same file — and C# reports that as an error the consumer resolves
  with an alias, not as silent misbehaviour. To be confirmed during implementation: that nothing in this
  repo imports both.
* **`AssetProviderServiceConfig` is a `ScriptableObject`.** Unity binds a `.asset` to its script by
  the `.cs.meta` GUID, and resolves the class inside by file name. Renaming file and class together
  while preserving the `.meta` keeps existing assets bound; renaming only one of the two breaks them.
  Confirmed by diffing the GUID before and after — see §7.
* No name collisions exist inside the repo — all twelve target names were confirmed unused before the
  design was accepted.

## 4. Part B — Moq for the interaction tests

### 4.1 Getting Moq into a Unity project

Moq is not currently present in this project in any form. It arrives as
[`nuget.moq`](https://docs.unity3d.com/Packages/nuget.moq@2.0/manual/index.html) `2.0.1` from the
Unity registry — Unity's own repackaging of Moq **4.18.2**, which predates the SponsorLink component
added in Moq 4.20 and so carries no telemetry.

It is added to `Packages/manifest.json` — the sandbox project's own dependencies — and **not** to any
package's `package.json`. This matters:

* A test-only dependency in `dependencies` would force every consumer to download Moq at runtime for
  a package they only ever use in a player build. Unity's own packages avoid this by splitting tests
  into a separate `*.tests` package referenced through the informational `relatedPackages` field.
  This repo ships `Tests/` inside each package instead, so the equivalent restraint is to declare
  Moq at the project level only.
* The consequence, accepted: a consumer who adds one of these packages to `testables` *and* wants to
  compile its tests must add `nuget.moq` themselves. That is a rare, deliberate act, and the
  alternative imposes a real cost on every ordinary consumer.

Every test assembly in this repo sets `"overrideReferences": true`, which means only the assemblies
named in `precompiledReferences` are visible. Each converted test asmdef therefore gains the three
DLLs the `nuget.moq` documentation names:

```json
"precompiledReferences": [
    "nunit.framework.dll",
    "Moq.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Threading.Tasks.Extensions.dll"
]
```

Six of the ten test assemblies need this: ComponentSystem, EventManagement, InventorySystem,
PersistentDataManagement, ShopManagement, UpdateManagement.

**Open risk:** `System.Runtime.CompilerServices.Unsafe.dll` is shipped by more than one package in
the Unity ecosystem, and Unity errors on two precompiled assemblies with the same name. Whether that
bites here is not knowable from documentation; it is settled by the first compile.

### 4.2 Which doubles become mocks

The rule: **Moq only where the assertion is the interaction.** If a test asserts on a double's state,
or passes it around by identity, it wants a fake.

| Package | Double | Why it is a mock |
|---|---|---|
| `UpdateManagement` | `UpdatableMock` | `UpdateCallCount()` / `IsUpdated()`; also needs to throw on demand → `Setup(...).Throws(...)`, `Verify(..., Times.Exactly(3))` |
| `PersistentDataManagement` | `PersistentDataSerializerMock` | `IsSerializedCalledOnce()` / `IsDeserializedCalledOnce()` plus argument capture |
| `PersistentDataManagement` | `PersistentDataIOStreamFactoryMock` | Per-channel call counting → `Verify(f => f.CreateWriteStreamFor(ch), Times.Once)` |
| `PersistentDataManagement` | `PersistentDataWrapperMock` | `Clear()` call counting and call-*ordering* assertions; ~60 of its 96 lines are unused stubs |
| `ShopManagement` | `PurchaseHandlerMock` | Argument capture (`givenShopPackage`) plus configurable success/failure |
| `ComponentSystem` | `CacheMock` | Asserts `TryCache` received A, B, C **in order** |
| `InventorySystem` | `MockInventoryItemConstraint` | Asserts `ApplyTo` received the running value; must also pass it through → `Returns<int>(v => v)` |
| `EventManagement` | `ListenerMock` | Asserts `OnEvent` was, or was not, called with a given event → `Times.Never` reads better than a null check |

`CacheMock` converts by constructing `CacheableEntity<ICache>` rather than
`CacheableEntity<CacheMock>`; `ICache` satisfies the `where T : ICache` constraint, and verification
happens on the mock directly instead of through `entity.Cache()`.

### 4.3 Which doubles stay fakes

| Package | From | To | Why it stays a fake |
|---|---|---|---|
| `ShopManagement` | `ShopPackageMock` | `FakeShopPackage` | Pure state — `Apply()` sets a flag `IsApplied()` reads |
| `ShopManagement` | `ShopPackageMockA` / `B` | `FakeShopPackageA` / `B` | **Must** be real distinct types. `PackagesOfType<T>()` and `AssignPurchaseHandler<T>()` dispatch on the concrete type, and a Moq proxy's type is generated |
| `EventManagement` | `EventMock` | `FakeGameEvent` | `IGameEvent` is an empty marker interface — there is nothing to verify |
| `ObjectPooling` | `MockObject` | `FakePoolable` | The pool constructs these itself in `CreateObject()`, and tests assert identity across acquire/release |

### 4.4 Consequences for releasing

None. The changelog rules exempt `Tests/`, and `Packages/manifest.json` sits at the `Packages/` root
rather than inside a package, so no `missing-entry` fires. **Part B needs no CHANGELOG entries and no
version bumps** — it changes nothing a consumer receives.

## 5. Order of work

Part A first, then Part B, as two separate commits on one branch cut from `dev`
(`refactor/drop-basic-prefix-and-moq`), landing in a single pull request back into `dev`.

They are not two pull requests. Part B rewrites the very test files Part A renames, so it cannot
merge without A ahead of it; stacking two pull requests would add coordination without adding any
reviewability that two well-separated commits do not already give.

The order is not arbitrary. Part A renames test fixtures in the same files Part B rewrites. Running
the mechanical, tool-driven rename first keeps its diff reviewable, and lets the Moq conversion be
written against final names instead of names that change underneath it.

## 6. Decisions taken

| Decision | Chosen | Alternative rejected |
|---|---|---|
| How Moq enters the repo | `nuget.moq` from the Unity registry, declared in `Packages/manifest.json` | Vendoring DLLs (one shared copy needed, licensing to track); NuGetForUnity (extra tool dependency) |
| Which doubles convert | Only those whose assertion is the interaction | Converting all fifteen — several are fakes, and three *cannot* be mocks |
| Rename scope | All twelve, including `CacheableBasicEntity` and the `ScriptableObject` | Only the ten clean interface implementations, leaving `Basic` in two public names |
| Breaking-change handling | Hard rename, `### Removed` entries, no shims | `[Obsolete]` forwarding types for one release — 12 extra shipped types and a cleanup pass, and it does not work for the `ScriptableObject` |
| Releasing | Left to a separate human decision | Bumping and tagging as part of this work |

## 7. Verification

* `unity test --mode EditMode` green after each part, on the Editor named in `ProjectVersion.txt`
  (no `-e`, no `--allow-install`), or `unity command run_tests --mode EditMode` against a live Editor.
* `node Tools/upm-release.mjs validate` — catches a `.meta` lost during a file rename.
* `node Tools/changelog-check.mjs --base dev --head HEAD` — catches a missing `### Removed` entry.
* `git diff` on the twelve `.cs.meta` files confirms every GUID is unchanged.
