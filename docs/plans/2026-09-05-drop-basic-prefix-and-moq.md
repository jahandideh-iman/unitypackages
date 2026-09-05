# Dropping the `Basic` prefix, and Moq for the interaction tests — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the twelve `Basic*` implementations to the name of the interface they implement, then replace the eight hand-written test doubles whose assertion *is* an interaction with Moq, leaving the rest as explicitly named fakes.

**Architecture:** Two sequential parts on one branch. Part A is a pure rename — no behaviour changes, driven by Roslyn so generic constraints and cross-package references follow, with each `.cs` and its `.meta` moved together to preserve Unity GUIDs. Part B adds `nuget.moq` at the project level only, then converts test doubles package by package.

**Tech Stack:** Unity 6000.2 (see `ProjectSettings/ProjectVersion.txt`), C# with `-nullable:enable`, NUnit via `com.unity.test-framework`, `nuget.moq` 2.0.1 (Moq 4.18.2), Node for the release/changelog tooling, `sharplens` MCP for Roslyn renames.

**Spec:** [`docs/specs/2026-09-05-basic-rename-and-moq-design.md`](../specs/2026-09-05-basic-rename-and-moq-design.md)

## Global Constraints

- **Branch from `dev`, never `master`.** Branch for this work: `refactor/drop-basic-prefix-and-moq`. One PR, into `dev`.
- **This is a refactor, not a feature. Do not write new failing tests first.** The existing suite is the specification. Every task's cycle is: suite green → make the change → suite green, with the *same* set of test names passing before and after unless the task says otherwise. A task that reduces the passing test count has broken something.
- **Every `.cs` rename moves its `.cs.meta` in the same commit.** `git mv` both. Never delete and recreate a `.meta` — the GUID inside binds consumer assets and asmdef references.
- **Never hand-edit `.unity`, `.prefab`, or `.asset` YAML.** Not needed by this plan; if a task appears to need it, stop and report.
- **Run the Editor named in `ProjectSettings/ProjectVersion.txt`.** No `-e`, no `--allow-install`.
- **Moq goes in `Packages/manifest.json` only.** Never in any package's `package.json`.
- **`### Removed` CHANGELOG entries go under `## [Unreleased]`** in each package Part A touches. Never create an empty `## [Unreleased]` heading — CI fails on one repo-wide.
- **Do not run `Tools/upm-release.mjs prepare`, do not bump versions, do not tag.** Releasing is out of scope.
- **Three package directories contain spaces** — `Asset Providing`, `Scene Management`, `UI Management`. Always quote paths.
- Use `git -C <path> ...` rather than prefixing git with `cd`.

## Test commands

An Editor is usually already open on this project, in which case Unity refuses a second instance. Pick accordingly:

```powershell
# Editor already open (fast, runs in the live instance, returns JSON):
unity command run_tests --mode EditMode

# No Editor open (CI, fresh checkout):
unity test --mode EditMode --output Library/editmode-results.xml
```

Exit codes for `unity test`: `0` success, `8` tests ran and failed, `6` the run never produced results (compiler errors). **`6` and `8` mean different things — do not treat `6` as a red suite.**

Repo tooling, no Unity needed:

```powershell
node Tools/upm-release.mjs validate
node Tools/changelog-check.mjs --base dev --head HEAD
```

## Baseline

Before Task 1, record the baseline so "same tests pass" is checkable:

```powershell
unity command run_tests --mode EditMode
```

Write the total/passed/failed counts into the task notes. Every later task compares against them.

---

## Part A — dropping the `Basic` prefix

### Task 1: PackageBasics — `BasicContainer<T>` → `Container<T>`

Goes first: `PersistentDataManagement` constructs `BasicContainer<T>` directly, so this rename must land before that package compiles cleanly.

**Files:**
- Rename: `Packages/PackageBasics/Runtime/Scripts/BasicContainer.cs` → `Container.cs` (+ `.meta`)
- Modify: `Packages/PersistentDataManagement/Runtime/Scripts/BasicPersistentDataManager.cs:24,166`
- Modify: `Packages/PackageBasics/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.PackageBasics.Container<T> : IContainer<T>` — the type Task 7 and any later task refers to.

- [ ] **Step 1: Confirm the suite is green at baseline**

Run: `unity command run_tests --mode EditMode`
Expected: the baseline counts recorded above. If it is already red, stop and report — do not rename on top of a broken suite.

- [ ] **Step 2: Rename the symbol with Roslyn**

Use `mcp__sharplens__rename_symbol` on `BasicContainer` in `Packages/PackageBasics/Runtime/Scripts/BasicContainer.cs` line 5, new name `Container`. This updates the two call sites in `BasicPersistentDataManager.cs` as well.

If sharplens cannot load the solution, generate it first: `unity command menu --path "Assets/Open C# Project"`.

- [ ] **Step 3: Move the file and its meta together**

```bash
git -C . mv Packages/PackageBasics/Runtime/Scripts/BasicContainer.cs      Packages/PackageBasics/Runtime/Scripts/Container.cs
git -C . mv Packages/PackageBasics/Runtime/Scripts/BasicContainer.cs.meta Packages/PackageBasics/Runtime/Scripts/Container.cs.meta
```

- [ ] **Step 4: Verify the GUID survived**

```bash
git -C . diff --cached -- Packages/PackageBasics/Runtime/Scripts/Container.cs.meta
```

Expected: the file shows as a pure rename, **no change to the `guid:` line**. If the GUID changed, undo and redo the move — a new GUID unbinds every consumer asset.

- [ ] **Step 5: Add the CHANGELOG entry**

In `Packages/PackageBasics/CHANGELOG.md`, under `## [Unreleased]` (create the heading directly below the intro paragraph if absent), add:

```markdown
### Removed

- `BasicContainer<T>`, renamed to `Container<T>`. It was the only implementation of `IContainer<T>`, so the `Basic` prefix distinguished it from nothing.
```

If `## [Unreleased]` already exists with a `### Removed` section, append the bullet rather than adding a second heading.

- [ ] **Step 6: Run the suite**

Run: `unity command run_tests --mode EditMode`
Expected: same counts as baseline. Zero compile errors.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(package-basics): rename BasicContainer to Container"
```

---

### Task 2: ComponentSystem — three renames

**Files:**
- Rename: `Packages/ComponentSystem/Runtime/Scripts/BasicEntity.cs` → `Entity.cs` (+ `.meta`)
- Rename: `Packages/ComponentSystem/Runtime/Scripts/BasicSpecializedEntity.cs` → `SpecializedEntity.cs` (+ `.meta`)
- Rename: `Packages/ComponentSystem/Runtime/Scripts/CacheableBasicEntity.cs` → `CacheableEntity.cs` (+ `.meta`)
- Rename: `Packages/ComponentSystem/Tests/Editor/UnitTests/BasicEntityTest.cs` → `EntityTest.cs` (+ `.meta`)
- Rename: `Packages/ComponentSystem/Tests/Editor/UnitTests/CacheableBasicEntityTest.cs` → `CacheableEntityTest.cs` (+ `.meta`)
- Modify: `Packages/ComponentSystem/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.ComponentSystem.Entity : IEntity`, `SpecializedEntity<T> : ISpecializedEntity<T>`, `CacheableEntity<T> : Entity where T : ICache`. Task 16 constructs `CacheableEntity<ICache>`.

- [ ] **Step 1: Rename the three runtime symbols with Roslyn**

`mcp__sharplens__rename_symbol`, one call each:
- `BasicEntity` → `Entity`
- `BasicSpecializedEntity` → `SpecializedEntity`
- `CacheableBasicEntity` → `CacheableEntity`

Do `BasicEntity` **last** of the three, or Roslyn will rewrite `CacheableBasicEntity`'s base clause mid-rename and the other two renames will target stale text.

- [ ] **Step 2: Rename the two test fixtures**

`BasicEntityTest` → `EntityTest`, `CacheableBasicEntityTest` → `CacheableEntityTest`.

- [ ] **Step 3: Move all five files and their metas**

```bash
cd Packages/ComponentSystem
git mv Runtime/Scripts/BasicEntity.cs                     Runtime/Scripts/Entity.cs
git mv Runtime/Scripts/BasicEntity.cs.meta                Runtime/Scripts/Entity.cs.meta
git mv Runtime/Scripts/BasicSpecializedEntity.cs          Runtime/Scripts/SpecializedEntity.cs
git mv Runtime/Scripts/BasicSpecializedEntity.cs.meta     Runtime/Scripts/SpecializedEntity.cs.meta
git mv Runtime/Scripts/CacheableBasicEntity.cs            Runtime/Scripts/CacheableEntity.cs
git mv Runtime/Scripts/CacheableBasicEntity.cs.meta       Runtime/Scripts/CacheableEntity.cs.meta
git mv Tests/Editor/UnitTests/BasicEntityTest.cs          Tests/Editor/UnitTests/EntityTest.cs
git mv Tests/Editor/UnitTests/BasicEntityTest.cs.meta     Tests/Editor/UnitTests/EntityTest.cs.meta
git mv Tests/Editor/UnitTests/CacheableBasicEntityTest.cs      Tests/Editor/UnitTests/CacheableEntityTest.cs
git mv Tests/Editor/UnitTests/CacheableBasicEntityTest.cs.meta Tests/Editor/UnitTests/CacheableEntityTest.cs.meta
```

- [ ] **Step 4: Verify all five GUIDs survived**

```bash
git diff --cached --stat -- '*.meta'
```

Expected: five `.meta` entries, all showing as renames with `0` insertions and `0` deletions.

- [ ] **Step 5: Add the CHANGELOG entry**

`Packages/ComponentSystem/CHANGELOG.md`, under `## [Unreleased]`:

```markdown
### Removed

- `BasicEntity`, `BasicSpecializedEntity<T>` and `CacheableBasicEntity<T>`, renamed to `Entity`, `SpecializedEntity<T>` and `CacheableEntity<T>`. Each was the only implementation of its interface, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 6: Run the suite**

Run: `unity command run_tests --mode EditMode`
Expected: baseline counts.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(component-system): drop the Basic prefix from Entity types"
```

---

### Task 3: ConfigurationManagement — `BasicConfigurationManager` → `ConfigurationManager`

**Files:**
- Rename: `Packages/ConfigurationManagement/Runtime/Scripts/BasicConfigurationManager.cs` → `ConfigurationManager.cs` (+ `.meta`)
- Rename: `Packages/ConfigurationManagement/Tests/Editor/UnitTests/BasicConfigurationManagerTest.cs` → `ConfigurationManagerTest.cs` (+ `.meta`)
- Modify: `Packages/ConfigurationManagement/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.ConfigurationManagement.ConfigurationManager : IConfigurationManager`.

- [ ] **Step 1: Rename both symbols with Roslyn**

`BasicConfigurationManager` → `ConfigurationManager`, then `BasicConfigurationManagerTest` → `ConfigurationManagerTest`.

`FakeConfigurer<T>` and `FakeMultiConfigurerAB` in that test file are **not** touched by this plan — they are already correctly named fakes.

- [ ] **Step 2: Move both files and their metas**

```bash
cd Packages/ConfigurationManagement
git mv Runtime/Scripts/BasicConfigurationManager.cs      Runtime/Scripts/ConfigurationManager.cs
git mv Runtime/Scripts/BasicConfigurationManager.cs.meta Runtime/Scripts/ConfigurationManager.cs.meta
git mv Tests/Editor/UnitTests/BasicConfigurationManagerTest.cs      Tests/Editor/UnitTests/ConfigurationManagerTest.cs
git mv Tests/Editor/UnitTests/BasicConfigurationManagerTest.cs.meta Tests/Editor/UnitTests/ConfigurationManagerTest.cs.meta
```

- [ ] **Step 3: Verify GUIDs survived**

`git diff --cached --stat -- '*.meta'` — two renames, no content change.

- [ ] **Step 4: Add the CHANGELOG entry**

```markdown
### Removed

- `BasicConfigurationManager`, renamed to `ConfigurationManager`. It was the only implementation of `IConfigurationManager`, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 5: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(configuration-management): rename BasicConfigurationManager to ConfigurationManager"
```

Expected: baseline counts before committing.

---

### Task 4: EventManagement — `BasicEventManager` → `EventManager`

**Files:**
- Rename: `Packages/EventManagement/Runtime/Scripts/BasicEventManager.cs` → `EventManager.cs` (+ `.meta`)
- Rename: `Packages/EventManagement/Tests/Editor/UnitTests/Foundation/EventManagement/BasicEventManagerTest.cs` → `EventManagerTest.cs` (+ `.meta`)
- Modify: `Packages/EventManagement/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.EventManagement.EventManager : IEventManager`. Task 18 rewrites the doubles inside `EventManagerTest.cs`.

**Note:** the test fixture class inside `BasicEventManagerTest.cs` is already called `EventManagerTest` — only the *file* name carries `Basic`. Rename the file; the class needs no change.

- [ ] **Step 1: Rename the runtime symbol with Roslyn**

`BasicEventManager` → `EventManager`.

- [ ] **Step 2: Move both files and their metas**

```bash
cd Packages/EventManagement
git mv Runtime/Scripts/BasicEventManager.cs      Runtime/Scripts/EventManager.cs
git mv Runtime/Scripts/BasicEventManager.cs.meta Runtime/Scripts/EventManager.cs.meta
git mv "Tests/Editor/UnitTests/Foundation/EventManagement/BasicEventManagerTest.cs"      "Tests/Editor/UnitTests/Foundation/EventManagement/EventManagerTest.cs"
git mv "Tests/Editor/UnitTests/Foundation/EventManagement/BasicEventManagerTest.cs.meta" "Tests/Editor/UnitTests/Foundation/EventManagement/EventManagerTest.cs.meta"
```

- [ ] **Step 3: Verify GUIDs survived**

`git diff --cached --stat -- '*.meta'` — two renames, no content change.

- [ ] **Step 4: Add the CHANGELOG entry**

```markdown
### Removed

- `BasicEventManager`, renamed to `EventManager`. It was the only implementation of `IEventManager`, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 5: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(event-management): rename BasicEventManager to EventManager"
```

---

### Task 5: InventorySystem — `BasicInventory<T>` → `Inventory<T>`

**Files:**
- Rename: `Packages/InventorySystem/Runtime/Scripts/BasicInventory.cs` → `Inventory.cs` (+ `.meta`)
- Rename: `Packages/InventorySystem/Tests/Editor/UnitTests/Game/InventorySystem/BasicInventoryTest.cs` → `InventoryTest.cs` (+ `.meta`)
- Modify: `Packages/InventorySystem/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.InventorySystem.Inventory<T> : IInventory<T> where T : IInventoryItem`. Task 17 rewrites the double used inside `InventoryTest.cs`.

**Note:** `EmptyConstraint`, declared at `BasicInventory.cs:7`, moves with the file and keeps its name. It is a runtime null-object, out of scope.

- [ ] **Step 1: Rename both symbols with Roslyn**

`BasicInventory` → `Inventory`, then `BasicInventoryTest` → `InventoryTest`.

- [ ] **Step 2: Move both files and their metas**

```bash
cd Packages/InventorySystem
git mv Runtime/Scripts/BasicInventory.cs      Runtime/Scripts/Inventory.cs
git mv Runtime/Scripts/BasicInventory.cs.meta Runtime/Scripts/Inventory.cs.meta
git mv "Tests/Editor/UnitTests/Game/InventorySystem/BasicInventoryTest.cs"      "Tests/Editor/UnitTests/Game/InventorySystem/InventoryTest.cs"
git mv "Tests/Editor/UnitTests/Game/InventorySystem/BasicInventoryTest.cs.meta" "Tests/Editor/UnitTests/Game/InventorySystem/InventoryTest.cs.meta"
```

- [ ] **Step 3: Verify GUIDs survived, add CHANGELOG entry**

```markdown
### Removed

- `BasicInventory<T>`, renamed to `Inventory<T>`. It was the only implementation of `IInventory<T>`, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 4: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(inventory-system): rename BasicInventory to Inventory"
```

---

### Task 6: ObjectPooling — `BasicObjectPool<T>` → `ObjectPool<T>`

**Files:**
- Rename: `Packages/ObjectPooling/Runtime/Scripts/BasicObjectPool.cs` → `ObjectPool.cs` (+ `.meta`)
- Modify: `Packages/ObjectPooling/Runtime/Scripts/UnityComponentObjectPool.cs:6` (its base clause)
- Rename: `Packages/ObjectPooling/Tests/Editor/UnitTests/ObjectPooling/BasicObjectPoolTest.cs` → `ObjectPoolTest.cs` (+ `.meta`)
- Modify: `Packages/ObjectPooling/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.ObjectPooling.ObjectPool<T> : IObjectPool<T> where T : IPoolable` — **abstract**, with `protected abstract T CreateObject()`, `protected abstract void ActivateObject(T)`, `protected abstract void DeactivateObject(T)`. Task 19 subclasses it as `TestableObjectPool`.

**Note on the name:** `Arman.ObjectPooling.ObjectPool<T>` shares a simple name with `UnityEngine.Pool.ObjectPool<T>`. Nothing in this repo imports `UnityEngine.Pool` — confirmed before the design was accepted — so no ambiguity arises here. `ScriptableObjectPool<T>`, `MonobehaviorObjectPool<T>` and `UnityComponentObjectPool<T>` in the same namespace are unaffected; only the last derives from the renamed type.

- [ ] **Step 1: Rename both symbols with Roslyn**

`BasicObjectPool` → `ObjectPool`, then `BasicObjectPoolTest` → `ObjectPoolTest`. Roslyn updates `UnityComponentObjectPool`'s base clause and `TestableBasicObjectPool`'s base clause automatically.

Leave `TestableBasicObjectPool` named as it is for now — Task 19 renames it as part of the double cleanup.

- [ ] **Step 2: Move both files and their metas**

```bash
cd Packages/ObjectPooling
git mv Runtime/Scripts/BasicObjectPool.cs      Runtime/Scripts/ObjectPool.cs
git mv Runtime/Scripts/BasicObjectPool.cs.meta Runtime/Scripts/ObjectPool.cs.meta
git mv Tests/Editor/UnitTests/ObjectPooling/BasicObjectPoolTest.cs      Tests/Editor/UnitTests/ObjectPooling/ObjectPoolTest.cs
git mv Tests/Editor/UnitTests/ObjectPooling/BasicObjectPoolTest.cs.meta Tests/Editor/UnitTests/ObjectPooling/ObjectPoolTest.cs.meta
```

- [ ] **Step 3: Verify GUIDs survived, add CHANGELOG entry**

```markdown
### Removed

- `BasicObjectPool<T>`, renamed to `ObjectPool<T>`. It was the only base implementation of `IObjectPool<T>`, so the `Basic` prefix distinguished it from nothing. Note that the new name shares its simple name with `UnityEngine.Pool.ObjectPool<T>`; a file importing both namespaces needs a `using` alias.
```

- [ ] **Step 4: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(object-pooling): rename BasicObjectPool to ObjectPool"
```

---

### Task 7: PersistentDataManagement — `BasicPersistentDataManager` → `PersistentDataManager`

**Files:**
- Rename: `Packages/PersistentDataManagement/Runtime/Scripts/BasicPersistentDataManager.cs` → `PersistentDataManager.cs` (+ `.meta`)
- Rename, all in `Packages/PersistentDataManagement/Tests/Editor/UnitTests/` (each + `.meta`):
  - `BasicPersistentDataManagerTestContext.cs` → `PersistentDataManagerTestContext.cs`
  - `BasicPersistentDataManagerTest_Saving.cs` → `PersistentDataManagerTest_Saving.cs`
  - `BasicPersistentDataManagerTest_Loading.cs` → `PersistentDataManagerTest_Loading.cs`
  - `BasicPersistentDataManagerTest_Deleting.cs` → `PersistentDataManagerTest_Deleting.cs`
  - `BasicPersistentDataManagerTest_Registering.cs` → `PersistentDataManagerTest_Registering.cs`
- Modify: `Packages/PersistentDataManagement/CHANGELOG.md`

**Interfaces:**
- Consumes: `Arman.PackageBasics.Container<T>` from Task 1.
- Produces: `Arman.PersistentDataManagement.PersistentDataManager : IPersistentDataManager`, and the protected fixture base `PersistentDataManagerTestContext` with members `manager`, `emptyStreamFactory`, `emptyDataWrapper`, `serializerA`, `serializerB`, `channel1`, `channel2`, `CreateManager(...)`, `InternalSetup()`. Task 14 rewrites that base's serializer fields.

- [ ] **Step 1: Rename the six symbols with Roslyn**

`BasicPersistentDataManager` → `PersistentDataManager`, then each of the five test classes, dropping `Basic` from each name.

- [ ] **Step 2: Move all six files and their metas**

```bash
cd Packages/PersistentDataManagement
git mv Runtime/Scripts/BasicPersistentDataManager.cs      Runtime/Scripts/PersistentDataManager.cs
git mv Runtime/Scripts/BasicPersistentDataManager.cs.meta Runtime/Scripts/PersistentDataManager.cs.meta
for n in TestContext Test_Saving Test_Loading Test_Deleting Test_Registering; do
  git mv "Tests/Editor/UnitTests/BasicPersistentDataManager$n.cs"      "Tests/Editor/UnitTests/PersistentDataManager$n.cs"
  git mv "Tests/Editor/UnitTests/BasicPersistentDataManager$n.cs.meta" "Tests/Editor/UnitTests/PersistentDataManager$n.cs.meta"
done
```

- [ ] **Step 3: Verify all six GUIDs survived, add CHANGELOG entry**

```markdown
### Removed

- `BasicPersistentDataManager`, renamed to `PersistentDataManager`. It was the only implementation of `IPersistentDataManager`, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 4: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(persistent-data-management): rename BasicPersistentDataManager to PersistentDataManager"
```

---

### Task 8: ShopManagement — `BasicShopCenter` → `ShopCenter`

**Files:**
- Rename: `Packages/ShopManagement/Runtime/Scripts/BasicShopCenter.cs` → `ShopCenter.cs` (+ `.meta`)
- Rename: `Packages/ShopManagement/Tests/Editor/UnitTests/Foundation/ShopManagement/BasicShopCenterTest.cs` → `ShopCenterTest.cs` (+ `.meta`)
- Modify: `Packages/ShopManagement/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.ShopManagement.ShopCenter : IShopCenter`. Task 15 rewrites the doubles used by `ShopCenterTest.cs`.

- [ ] **Step 1: Rename both symbols with Roslyn, move both files and their metas**

```bash
cd Packages/ShopManagement
git mv Runtime/Scripts/BasicShopCenter.cs      Runtime/Scripts/ShopCenter.cs
git mv Runtime/Scripts/BasicShopCenter.cs.meta Runtime/Scripts/ShopCenter.cs.meta
git mv "Tests/Editor/UnitTests/Foundation/ShopManagement/BasicShopCenterTest.cs"      "Tests/Editor/UnitTests/Foundation/ShopManagement/ShopCenterTest.cs"
git mv "Tests/Editor/UnitTests/Foundation/ShopManagement/BasicShopCenterTest.cs.meta" "Tests/Editor/UnitTests/Foundation/ShopManagement/ShopCenterTest.cs.meta"
```

- [ ] **Step 2: Verify GUIDs survived, add CHANGELOG entry**

```markdown
### Removed

- `BasicShopCenter`, renamed to `ShopCenter`. It was the only implementation of `IShopCenter`, so the `Basic` prefix distinguished it from nothing.
```

- [ ] **Step 3: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(shop-management): rename BasicShopCenter to ShopCenter"
```

---

### Task 9: UpdateManagement — `BasicUpdateManager` → `UpdateManager`

**Files:**
- Rename: `Packages/UpdateManagement/Runtime/Scripts/BasicUpdateManager.cs` → `UpdateManager.cs` (+ `.meta`)
- Rename: `Packages/UpdateManagement/Tests/Editor/UnitTests/BasicUpdateManagerTest_ThrowingUpdatables.cs` → `UpdateManagerTest_ThrowingUpdatables.cs` (+ `.meta`)
- Modify: `Packages/UpdateManagement/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `Arman.UpdateManagement.UpdateManager : IUpdateManager`. Task 13 rewrites the double used by both UpdateManagement fixtures.

**Note:** `UpdateManagerTest_ChannelStateChangedEvent.cs` already has the target name and only needs its `new BasicUpdateManager()` call updated — Roslyn handles that. There is also a `UnityUpdateManager` in this package; it is a separate MonoBehaviour adapter and is **not** renamed.

- [ ] **Step 1: Rename both symbols with Roslyn, move both files and their metas**

```bash
cd Packages/UpdateManagement
git mv Runtime/Scripts/BasicUpdateManager.cs      Runtime/Scripts/UpdateManager.cs
git mv Runtime/Scripts/BasicUpdateManager.cs.meta Runtime/Scripts/UpdateManager.cs.meta
git mv Tests/Editor/UnitTests/BasicUpdateManagerTest_ThrowingUpdatables.cs      Tests/Editor/UnitTests/UpdateManagerTest_ThrowingUpdatables.cs
git mv Tests/Editor/UnitTests/BasicUpdateManagerTest_ThrowingUpdatables.cs.meta Tests/Editor/UnitTests/UpdateManagerTest_ThrowingUpdatables.cs.meta
```

- [ ] **Step 2: Verify GUIDs survived, add CHANGELOG entry**

The `## [Unreleased]` heading in this package already exists with `### Added` and `### Fixed` sections. Add a `### Removed` section to it:

```markdown
### Removed

- `BasicUpdateManager`, renamed to `UpdateManager`. It was the only pure implementation of `IUpdateManager`, so the `Basic` prefix distinguished it from nothing. The `UnityUpdateManager` MonoBehaviour adapter is unchanged.
```

- [ ] **Step 3: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(update-management): rename BasicUpdateManager to UpdateManager"
```

---

### Task 10: Asset Providing — `BasicAssetProviderServiceConfig` → `AssetProviderServiceConfig`

The one `ScriptableObject` in the set. It has no interface; it is included so `Basic` does not survive in a public name for no reason.

**Files:**
- Rename: `Packages/Asset Providing/Runtime/Scripts/BasicAssetProviderServiceConfig.cs` → `AssetProviderServiceConfig.cs` (+ `.meta`)
- Modify: `Packages/Asset Providing/CHANGELOG.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `AssetProviderServiceConfig : ScriptableObject`.

**Why the GUID matters more here than anywhere else:** Unity binds a `.asset` file to its script through the `m_Script` GUID, which lives in `BasicAssetProviderServiceConfig.cs.meta`, and then finds the class inside by matching the file name. Renaming the class without the file, or the file without carrying the `.meta`, orphans every `.asset` a consumer has authored. No `.asset` in this repo references it — checked — but consumers' projects are the real exposure.

- [ ] **Step 1: Record the current GUID**

```bash
grep guid: "Packages/Asset Providing/Runtime/Scripts/BasicAssetProviderServiceConfig.cs.meta"
```

Expected: `guid: 3bcad97f763701a40928281e4e7899c1`. Write it down.

- [ ] **Step 2: Rename the symbol with Roslyn, then move file and meta**

```bash
cd "Packages/Asset Providing"
git mv Runtime/Scripts/BasicAssetProviderServiceConfig.cs      Runtime/Scripts/AssetProviderServiceConfig.cs
git mv Runtime/Scripts/BasicAssetProviderServiceConfig.cs.meta Runtime/Scripts/AssetProviderServiceConfig.cs.meta
```

- [ ] **Step 3: Confirm the GUID is byte-for-byte unchanged**

```bash
grep guid: "Packages/Asset Providing/Runtime/Scripts/AssetProviderServiceConfig.cs.meta"
```

Expected: exactly `guid: 3bcad97f763701a40928281e4e7899c1`, the value from Step 1. **If it differs, stop and report** — Unity regenerated the meta and every consumer asset is now orphaned.

- [ ] **Step 4: Check the class name matches the new file name**

```bash
grep -n "class AssetProviderServiceConfig" "Packages/Asset Providing/Runtime/Scripts/AssetProviderServiceConfig.cs"
```

Expected: one hit. A `ScriptableObject` whose class name does not match its file name will not deserialize.

- [ ] **Step 5: Add the CHANGELOG entry**

```markdown
### Removed

- `BasicAssetProviderServiceConfig`, renamed to `AssetProviderServiceConfig`. The script GUID is unchanged, so existing `.asset` files stay bound, but any code referring to the type by name needs updating.
```

- [ ] **Step 6: Run the suite and commit**

```bash
unity command run_tests --mode EditMode
git add -A
git commit -m "refactor(asset-providing): rename BasicAssetProviderServiceConfig"
```

---

### Task 11: Part A sweep and verification

**Files:**
- Modify: any file still mentioning a renamed type (READMEs, `.agents/AGENTS.md`, package descriptions)

- [ ] **Step 1: Find every surviving mention**

```bash
grep -rn "BasicEntity\|BasicSpecializedEntity\|CacheableBasicEntity\|BasicConfigurationManager\|BasicEventManager\|BasicInventory\|BasicObjectPool\|BasicContainer\|BasicPersistentDataManager\|BasicShopCenter\|BasicUpdateManager\|BasicAssetProviderServiceConfig" --include=*.cs --include=*.md --include=*.json Packages/ Assets/ docs/ .agents/ README.md
```

Expected after fixing: hits only in CHANGELOG `### Removed` entries and in `docs/specs/2026-09-05-basic-rename-and-moq-design.md`, both of which *should* name the old types. Update every README or doc hit to the new name.

`JsonBasic` in `Packages/PackageBasics/Runtime/ThirdParties/NiceJson.cs` is vendored third-party code and must not appear in this sweep — the pattern above does not match it.

- [ ] **Step 2: Confirm no `.meta` was lost**

```bash
node Tools/upm-release.mjs validate
```

Expected: exit 0. This check exists precisely to catch a `.meta` dropped during a file rename.

- [ ] **Step 3: Confirm the changelog rules pass**

```bash
node Tools/changelog-check.mjs --base dev --head HEAD
```

Expected: exit 0. If it reports `missing-entry` for a package, that package's `### Removed` bullet is missing or empty.

- [ ] **Step 4: Full suite**

```bash
unity command run_tests --mode EditMode
```

Expected: baseline counts exactly. Part A changes no behaviour.

- [ ] **Step 5: Commit any sweep fixes**

```bash
git add -A
git commit -m "docs: update references to the renamed implementations"
```

---

## Part B — Moq for the interaction tests

### Task 12: Add `nuget.moq` and prove it compiles

The smallest vertical slice that proves the dependency works, before eight conversions depend on it. Do not convert any test in this task.

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Packages/UpdateManagement/Tests/Editor/Arman.UpdateManagement.Tests.Editor.asmdef`
- Create: `Packages/UpdateManagement/Tests/Editor/UnitTests/MoqSmokeTest.cs` (+ `.meta`, generated by Unity) — **deleted again in Step 6**

**Interfaces:**
- Consumes: nothing.
- Produces: a working `Moq` reference in `Arman.UpdateManagement.Tests.Editor`, and the exact `precompiledReferences` block Tasks 14–18 copy.

- [ ] **Step 1: Add the dependency**

In `Packages/manifest.json`, add to `dependencies`, keeping the block's existing alphabetical-ish grouping with the other non-`com.unity.modules` entries:

```json
"nuget.moq": "2.0.1",
```

The id is `nuget.moq`, **not** `com.unity.nuget.moq` — the latter does not exist on the registry. It resolves from `https://packages.unity.com` with no scoped-registry configuration.

- [ ] **Step 2: Let Unity resolve it, then confirm**

```bash
unity command eval --code "UnityEditor.PackageManager.PackageInfo.FindForAssetPath(\"Packages/nuget.moq\")?.version"
```

Expected: `2.0.1`. If resolution fails, `Packages/packages-lock.json` will not gain an entry — check network access to `packages.unity.com`.

- [ ] **Step 3: Wire the test assembly**

In `Arman.UpdateManagement.Tests.Editor.asmdef`, replace the `precompiledReferences` array. The assembly sets `"overrideReferences": true`, so anything not listed here is invisible to it:

```json
    "precompiledReferences": [
        "nunit.framework.dll",
        "Moq.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Threading.Tasks.Extensions.dll"
    ],
```

- [ ] **Step 4: Write a throwaway smoke test**

`Packages/UpdateManagement/Tests/Editor/UnitTests/MoqSmokeTest.cs`:

```csharp
using Moq;
using NUnit.Framework;

namespace Arman.UpdateManagement.Tests
{
    public class MoqSmokeTest
    {
        [Test]
        public void MoqCanMockAnInterfaceAndVerifyACall()
        {
            var updatable = new Mock<IUpdatable>();

            updatable.Object.UpdateTime(1f);

            updatable.Verify(u => u.UpdateTime(1f), Times.Once);
        }
    }
}
```

- [ ] **Step 5: Run it**

```bash
unity command run_tests --mode EditMode --filter MoqSmokeTest
```

Expected: 1 test, passing.

**If instead you get a duplicate-assembly error naming `System.Runtime.CompilerServices.Unsafe.dll`** — this is the one risk the spec flagged as unknowable in advance. Another resolved package ships the same assembly. Try dropping that one entry from `precompiledReferences` and re-running; Unity may already be supplying it. If the conflict persists, stop and report — do not work around it by disabling `overrideReferences`, which would silently widen every test assembly's visible surface.

- [ ] **Step 6: Delete the smoke test and commit**

```bash
rm Packages/UpdateManagement/Tests/Editor/UnitTests/MoqSmokeTest.cs
rm -f Packages/UpdateManagement/Tests/Editor/UnitTests/MoqSmokeTest.cs.meta
git add -A
git commit -m "test: add nuget.moq and wire it into the UpdateManagement test assembly"
```

No CHANGELOG entry: `Tests/` and the root `Packages/manifest.json` are both exempt from `missing-entry`, and nothing a consumer receives has changed.

---

### Task 13: UpdateManagement — `UpdatableMock` → Moq

**Files:**
- Delete: `Packages/UpdateManagement/Tests/Editor/UnitTests/UpdatableMock.cs` (+ `.meta`)
- Modify: `Packages/UpdateManagement/Tests/Editor/UnitTests/UpdateManagerTest_ThrowingUpdatables.cs`

**Interfaces:**
- Consumes: `UpdateManager` (Task 9), the Moq reference (Task 12).
- Produces: nothing later tasks depend on.

`UpdatableMock` exists only to count `UpdateTime` calls and to throw on demand — both interactions. `IUpdatable` has one member, `void UpdateTime(float)`.

- [ ] **Step 1: Rewrite the fixture**

Replace the whole of `UpdateManagerTest_ThrowingUpdatables.cs` with:

```csharp
using System;
using System.Text.RegularExpressions;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UpdateManagerTest_ThrowingUpdatables
    {
        const string ThrownMessage = "updatable failed";

        static readonly Regex ExpectedLog = new Regex("InvalidOperationException: " + ThrownMessage);

        UpdateManager manager = null!;
        IChannel channel = null!;

        [SetUp]
        public void Setup()
        {
            manager = new UpdateManager();
            channel = new NamedChannel("Channel");
        }

        static Mock<IUpdatable> ThrowingUpdatable()
        {
            var throwing = new Mock<IUpdatable>();
            throwing
                .Setup(u => u.UpdateTime(It.IsAny<float>()))
                .Throws(new InvalidOperationException(ThrownMessage));
            return throwing;
        }

        [Test]
        public void AdvancingTimeShouldNotPropagateAnExceptionFromAnUpdatable()
        {
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            Assert.That(new TestDelegate(() => manager.AdvanceTime(1f)), Throws.Nothing);
        }

        [Test]
        public void AnExceptionFromAnUpdatableShouldBeLogged()
        {
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
        }

        [Test]
        public void AThrowingUpdatableShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var wellBehaved = new Mock<IUpdatable>();

            // Registered on both sides of the offender: the channel is walked back to
            // front, so one of these would be skipped whichever order it aborted in.
            var first = new Mock<IUpdatable>();
            manager.RegisterUpdatable(first.Object, channel);
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);
            manager.RegisterUpdatable(wellBehaved.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            first.Verify(u => u.UpdateTime(It.IsAny<float>()), Times.Once);
            wellBehaved.Verify(u => u.UpdateTime(It.IsAny<float>()), Times.Once);
        }

        [Test]
        public void AThrowingUpdatableShouldStayRegistered()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            Assert.That(manager.Has(throwing.Object), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldBeUpdatedAgainOnEveryLaterTick()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);

            throwing.Verify(u => u.UpdateTime(It.IsAny<float>()), Times.Exactly(3));
        }

        [Test]
        public void AnUpdatableThatDoesNotThrowShouldStayRegistered()
        {
            var wellBehaved = new Mock<IUpdatable>();

            manager.RegisterUpdatable(wellBehaved.Object, channel);
            manager.AdvanceTime(1f);

            Assert.That(manager.Has(wellBehaved.Object), Is.True);
            wellBehaved.Verify(u => u.UpdateTime(It.IsAny<float>()), Times.Once);
        }
    }
}
```

- [ ] **Step 2: Delete the double**

```bash
git rm Packages/UpdateManagement/Tests/Editor/UnitTests/UpdatableMock.cs
git rm Packages/UpdateManagement/Tests/Editor/UnitTests/UpdatableMock.cs.meta
```

- [ ] **Step 3: Run this package's tests**

```bash
unity command run_tests --mode EditMode --filter Arman.UpdateManagement.Tests
```

Expected: same test names, same count, all passing. `manager.Has(mock.Object)` works because Moq proxies have reference identity like any object.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(update-management): replace UpdatableMock with Moq"
```

---

### Task 14: PersistentDataManagement — three doubles → Moq

The largest conversion. `PersistentDataWrapperMock` alone is 96 lines, roughly 60 of them `NotImplementedException` stubs Moq supplies for free.

**Files:**
- Modify: `Packages/PersistentDataManagement/Tests/Editor/Arman.PersistentDataManagement.Tests.Editor.asmdef`
- Delete: `Packages/PersistentDataManagement/Tests/Editor/Mocks/PersistentDataSerializerMock.cs`, `PersistentDataIOStreamFactoryMock.cs`, `PersistentDataWrapperMock.cs` (+ `.meta` each), and the `Mocks/` folder + `Mocks.meta` if it ends up empty
- Modify: `PersistentDataManagerTestContext.cs`, `PersistentDataManagerTest_Saving.cs`, `PersistentDataManagerTest_Loading.cs`, `PersistentDataManagerTest_Deleting.cs`

**Interfaces:**
- Consumes: `PersistentDataManager` and `PersistentDataManagerTestContext` (Task 7), Moq (Task 12).
- Produces: a fixture base exposing `Mock<IPersistentDataSerializer> serializerA, serializerB` instead of the old concrete doubles.

- [ ] **Step 1: Wire the assembly**

Same `precompiledReferences` block as Task 12 Step 3.

- [ ] **Step 2: Rewrite the fixture base**

`PersistentDataManagerTestContext.cs`. The old `PersistentDataSerializerMock(key)` constructor set a key returned by `Key()`; Moq expresses that as a `Setup`.

```csharp
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataManagerTestContext
    {
        protected PersistentDataManager manager;

        // The collaborators the default manager was built with. A test that wants to
        // observe one of them builds a new manager, passing the other one through.
        protected IPersistentDataIOStreamFactory emptyStreamFactory;
        protected IPersistentDataWrapper emptyDataWrapper;

        protected Mock<IPersistentDataSerializer> serializerA;
        protected Mock<IPersistentDataSerializer> serializerB;

        protected IChannel channel1;
        protected IChannel channel2;

        [SetUp]
        public void Setup()
        {
            serializerA = Serializer("A");
            serializerB = Serializer("B");

            channel1 = new NamedChannel("ChannelA");
            channel2 = new NamedChannel("ChannelB");

            emptyStreamFactory = new EmptyPersistetDataIOStreamFactory();
            emptyDataWrapper = new EmptyPersistentDataWrapper();

            manager = CreateManager(emptyStreamFactory, emptyDataWrapper);

            InternalSetup();
        }

        protected static Mock<IPersistentDataSerializer> Serializer(string key)
        {
            var serializer = new Mock<IPersistentDataSerializer>();
            serializer.Setup(s => s.Key()).Returns(key);
            return serializer;
        }

        protected static PersistentDataManager CreateManager(
            IPersistentDataIOStreamFactory streamFactory,
            IPersistentDataWrapper dataWrapper,
            int saveVersion = 0)
        {
            return new PersistentDataManager(streamFactory, dataWrapper, saveVersion);
        }

        protected virtual void InternalSetup()
        {

        }
    }
}
```

- [ ] **Step 3: Translate the assertions**

Apply these mappings throughout the four fixture files. Every `serializerA` etc. usage becomes `serializerA.Object` when *passed* to the manager, and stays `serializerA` when *verified*.

| Old | New |
|---|---|
| `serializerA.IsSerializedCalledOnce()` is `True` | `serializerA.Verify(s => s.SerializeTo(It.IsAny<IWritablePersistentDataWrapper>()), Times.Once)` |
| `serializerB.IsSerializedCalledOnce()` is `False` | `serializerB.Verify(s => s.SerializeTo(It.IsAny<IWritablePersistentDataWrapper>()), Times.Never)` |
| `serializerA.IsSerialized()` is `False` | `serializerA.Verify(s => s.SerializeTo(It.IsAny<IWritablePersistentDataWrapper>()), Times.Never)` |
| `serializerA.IsDeserializedCalledOnce()` is `True` | `serializerA.Verify(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()), Times.Once)` |
| `serializerA.IsDeserialized()` is `False` | `serializerA.Verify(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()), Times.Never)` |
| `serializerA.onSerializeAction = w => …` | `serializerA.Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataWrapper>())).Callback<IWritablePersistentDataWrapper>(w => …)` |
| `serializerA.onDeserializeAction = w => …` | `serializerA.Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>())).Callback<IReadablePersistentDataWrapper>(w => …)` |
| `new PersistentDataWrapperMock()` | `WrapperMock()` — see Step 4 |
| `wrapper.onClearAction = () => clearCallCounts++` | drop the counter; `wrapper.Verify(w => w.Clear(), Times.Exactly(2))` |
| `wrapper.onWriteAction = w => writeStep = step` | `wrapper.Setup(w => w.WriteTo(It.IsAny<StreamWriter>())).Callback(() => writeStep = step)` |
| `wrapper.onReadAction = s => readStep = step` | `wrapper.Setup(w => w.ReadFrom(It.IsAny<StreamReader>())).Callback(() => readStep = step)` |
| `streamFactory.CreateWriteStreamIsCalledOnceFor(ch)` is `True` | `streamFactory.Verify(f => f.CreateWriteStreamFor(ch), Times.Once)` |
| `streamFactory.CreateWriteStreamIsCalledOnceFor(ch)` is `False` | `streamFactory.Verify(f => f.CreateWriteStreamFor(ch), Times.Never)` |
| `streamFactory.CreateReadStreamIsCalledOnceFor(ch)` | `streamFactory.Verify(f => f.CreateReadStreamFor(ch), Times.Once)` |
| `streamFactory.DeleteIsCalledOnceFor(ch)` is `True` | `streamFactory.Verify(f => f.Delete(ch), Times.Once)` |
| `new PersistentDataIOStreamFactoryMock()` | `StreamFactoryMock()` — see Step 4 |

Assertions that check *identity* rather than interaction stay as they are — for example `Assert.That(givenWrappers[serializerA.Object], Is.SameAs(persistentDataWrapper.Object))`, and the `Throws.Exception.InstanceOf<...>` checks.

- [ ] **Step 4: Add the two mock builders to the fixture base**

The old hand-written doubles had non-default return values the tests rely on: `PersistentDataIOStreamFactoryMock.HasReadableStreamFor` returned `true`, and `PersistentDataWrapperMock` returned `this` from its fluent writers and `true` from `HasKey`. Moq returns `default` unless told otherwise, so these must be set up explicitly. Add to `PersistentDataManagerTestContext`:

```csharp
        protected static Mock<IPersistentDataIOStreamFactory> StreamFactoryMock()
        {
            var factory = new Mock<IPersistentDataIOStreamFactory>();
            factory.Setup(f => f.HasReadableStreamFor(It.IsAny<IChannel>())).Returns(true);
            return factory;
        }

        protected static Mock<IPersistentDataWrapper> WrapperMock()
        {
            var wrapper = new Mock<IPersistentDataWrapper>();
            wrapper.Setup(w => w.HasKey(It.IsAny<string>())).Returns(true);
            wrapper.Setup(w => w.WriteInt(It.IsAny<string>(), It.IsAny<int>())).Returns(() => wrapper.Object);
            wrapper.Setup(w => w.BeginWritingBlock(It.IsAny<string>())).Returns(() => wrapper.Object);
            wrapper.Setup(w => w.EndWritingBlock()).Returns(() => wrapper.Object);
            return wrapper;
        }
```

`Returns(() => wrapper.Object)` must be the lambda form, not `Returns(wrapper.Object)` — the latter dereferences `wrapper.Object` while the mock is still being built.

- [ ] **Step 5: Delete the three doubles**

```bash
cd Packages/PersistentDataManagement/Tests/Editor
git rm Mocks/PersistentDataSerializerMock.cs Mocks/PersistentDataSerializerMock.cs.meta
git rm Mocks/PersistentDataIOStreamFactoryMock.cs Mocks/PersistentDataIOStreamFactoryMock.cs.meta
git rm Mocks/PersistentDataWrapperMock.cs Mocks/PersistentDataWrapperMock.cs.meta
git rm Mocks.meta
```

Remove `Mocks.meta` only if `Mocks/` is now empty — check with `ls Mocks` first. A folder without its `.meta`, or a `.meta` without its folder, both fail `validate`.

- [ ] **Step 6: Run this package's tests**

```bash
unity command run_tests --mode EditMode --filter Arman.PersistentDataManagement.Tests
```

Expected: same test names, same count, all passing.

The two call-*ordering* tests (`…ShouldWriteDataToPersistentDataWrapperAfterCallingAllSerializers` and its Loading counterpart) are the ones most likely to fail first. They work by incrementing a shared `step` counter from each serializer callback and snapshotting it from the wrapper callback; that survives the translation as long as every `Callback` is registered *before* the manager is constructed.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "test(persistent-data-management): replace three hand-written mocks with Moq"
```

---

### Task 15: ShopManagement — `PurchaseHandlerMock` → Moq, shop packages → fakes

The one package where both halves of the rule appear together.

**Files:**
- Modify: `Packages/ShopManagement/Tests/Editor/Arman.ShopManagement.Editor.Tests.asmdef`
- Delete: `Packages/ShopManagement/Tests/Editor/Mocks/PurchaseHandlerMock.cs` (+ `.meta`)
- Rename: `Packages/ShopManagement/Tests/Editor/Mocks/ShopPackageMock.cs` → `FakeShopPackage.cs` (+ `.meta`)
- Modify: `Packages/ShopManagement/Tests/Editor/UnitTests/Foundation/ShopManagement/ShopCenterTest.cs`

**Interfaces:**
- Consumes: `ShopCenter` (Task 8), Moq (Task 12).
- Produces: `Arman.Mocks.Foundation.ShopManagement.Core.FakeShopPackage`, plus `FakeShopPackageA` / `FakeShopPackageB` declared in the test file.

**Why the packages stay fakes:** `ShopCenter.PackagesOfType<T>()` and `AssignPurchaseHandler<T>()` dispatch on the *concrete* type argument. A Moq proxy's runtime type is generated, so `PackagesOfType<Mock<IShopPackage>>()` cannot express what these tests check. They need real, distinct, named types.

- [ ] **Step 1: Wire the assembly**

Same `precompiledReferences` block as Task 12 Step 3.

- [ ] **Step 2: Rename the fake**

```bash
cd Packages/ShopManagement/Tests/Editor
git mv Mocks/ShopPackageMock.cs      Mocks/FakeShopPackage.cs
git mv Mocks/ShopPackageMock.cs.meta Mocks/FakeShopPackage.cs.meta
```

Then rewrite `Mocks/FakeShopPackage.cs`:

```csharp
using Arman.ShopManagement;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class FakeShopPackage : IShopPackage
    {
        bool isApplied = false;

        public bool IsApplied()
        {
            return isApplied;
        }

        public void Apply()
        {
            isApplied = true;
        }
    }
}
```

- [ ] **Step 3: Delete the purchase handler double**

```bash
git rm Mocks/PurchaseHandlerMock.cs Mocks/PurchaseHandlerMock.cs.meta
```

- [ ] **Step 4: Update the subclasses in the test file**

At the top of `ShopCenterTest.cs`, rename the two marker subclasses:

```csharp
    public class FakeShopPackageA : FakeShopPackage {}

    public class FakeShopPackageB : FakeShopPackage {}
```

Replace every `ShopPackageMock` / `ShopPackageMockA` / `ShopPackageMockB` in the file with `FakeShopPackage` / `FakeShopPackageA` / `FakeShopPackageB`, including inside the `PackagesOfType<…>()` and `AssignPurchaseHandler<…>()` type arguments.

- [ ] **Step 5: Convert the purchase handler to Moq**

Add `using Moq;`. `IPurchaseHandler.Purchase` takes `(IShopPackage, Action<IPurchaseSuccessResult>, Action<IPurchaseFailureResult>)`. The old double captured the package and invoked one of the two callbacks; Moq expresses both with a `Callback`. Add this helper to the fixture:

```csharp
        static Mock<IPurchaseHandler> PurchaseHandler(bool shouldSucceed)
        {
            var handler = new Mock<IPurchaseHandler>();
            handler
                .Setup(h => h.Purchase(
                    It.IsAny<IShopPackage>(),
                    It.IsAny<Action<IPurchaseSuccessResult>>(),
                    It.IsAny<Action<IPurchaseFailureResult>>()))
                .Callback<IShopPackage, Action<IPurchaseSuccessResult>, Action<IPurchaseFailureResult>>(
                    (package, onSuccess, onFailure) =>
                    {
                        if (shouldSucceed)
                            onSuccess(null);
                        else
                            onFailure(null);
                    });
            return handler;
        }
```

Then translate:

| Old | New |
|---|---|
| `new PurchaseHandlerMock()` | `PurchaseHandler(shouldSucceed: false)` |
| `handler.ShouldSucceed(true)` | `PurchaseHandler(shouldSucceed: true)` at construction |
| `handler.givenShopPackage` is `SameAs(packageA)` | `handler.Verify(h => h.Purchase(packageA, It.IsAny<Action<IPurchaseSuccessResult>>(), It.IsAny<Action<IPurchaseFailureResult>>()), Times.Once)` |
| `handler.givenShopPackage` is `Null` | same `Verify` with `Times.Never` |
| `handler.Clear()` then re-assert | drop the `Clear()`; assert with `Times.Once` against each specific package instead |

`AssignPurchaseHandler<FakeShopPackageA>(handler.Object)` — pass `.Object` at the call site.

The test `PurchasingShouldBeDelegatedToDesignatedPurchaseHandler` used `Clear()` to reset between two halves. With Moq, verify each half against its own package argument and drop the reset entirely:

```csharp
            shopCenter.Purchase(packageA, delegate { }, delegate { });
            shopCenter.Purchase(packageB, delegate { }, delegate { });

            packageAPurchaseHandler.Verify(h => h.Purchase(packageA, It.IsAny<Action<IPurchaseSuccessResult>>(), It.IsAny<Action<IPurchaseFailureResult>>()), Times.Once);
            packageAPurchaseHandler.Verify(h => h.Purchase(packageB, It.IsAny<Action<IPurchaseSuccessResult>>(), It.IsAny<Action<IPurchaseFailureResult>>()), Times.Never);
            packageBPurchaseHandler.Verify(h => h.Purchase(packageB, It.IsAny<Action<IPurchaseSuccessResult>>(), It.IsAny<Action<IPurchaseFailureResult>>()), Times.Once);
            packageBPurchaseHandler.Verify(h => h.Purchase(packageA, It.IsAny<Action<IPurchaseSuccessResult>>(), It.IsAny<Action<IPurchaseFailureResult>>()), Times.Never);
```

- [ ] **Step 6: Run this package's tests and commit**

```bash
unity command run_tests --mode EditMode --filter Arman.ShopManagement
git add -A
git commit -m "test(shop-management): Moq for the purchase handler, named fakes for the packages"
```

Expected: same test names, same count, all passing.

---

### Task 16: ComponentSystem — `CacheMock` → Moq

**Files:**
- Modify: `Packages/ComponentSystem/Tests/Editor/Arman.ComponentSystem.Editor.Tests.asmdef`
- Modify: `Packages/ComponentSystem/Tests/Editor/UnitTests/CacheableEntityTest.cs`

**Interfaces:**
- Consumes: `CacheableEntity<T>` (Task 2), Moq (Task 12).
- Produces: nothing later tasks depend on.

The single test asserts `TryCache` received `ComponentA`, `ComponentB`, `ComponentC` **in that order**. Moq expresses ordering with `MockSequence`, but the clearer translation here is three `Verify` calls plus a recorded list, because the assertion is about order *and* type.

- [ ] **Step 1: Wire the assembly**

Same `precompiledReferences` block as Task 12 Step 3.

- [ ] **Step 2: Rewrite the test**

The old test constructed `CacheableBasicEntity<CacheMock>` and read back `entity.Cache().components`. `ICache` satisfies the `where T : ICache` constraint directly, so construct `CacheableEntity<ICache>` and observe the mock instead:

```csharp
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace Arman.ComponentSystem.Tests
{
    public class CacheableEntityTest
    {
        [Test]
        public void AddingComponentShouldCallTryCache()
        {
            var cached = new List<IComponent>();

            var cache = new Mock<ICache>();
            cache
                .Setup(c => c.TryCache(It.IsAny<IComponent>()))
                .Callback<IComponent>(cached.Add);

            var entity = new CacheableEntity<ICache>(cache.Object);

            entity.AddComponents(
                new ComponentA(),
                new ComponentB(),
                new ComponentC());

            Assert.That(cached[0], Is.TypeOf<ComponentA>());
            Assert.That(cached[1], Is.TypeOf<ComponentB>());
            Assert.That(cached[2], Is.TypeOf<ComponentC>());
        }
    }
}
```

`ComponentA`, `ComponentB` and `ComponentC` are declared in `EntityTest.cs` in the same namespace and stay there — they are marker types, not doubles.

- [ ] **Step 3: Run this package's tests and commit**

```bash
unity command run_tests --mode EditMode --filter Arman.ComponentSystem
git add -A
git commit -m "test(component-system): replace CacheMock with Moq"
```

---

### Task 17: InventorySystem — `MockInventoryItemConstraint` → Moq

**Files:**
- Modify: `Packages/InventorySystem/Tests/Editor/Arman.InventorySystem.Tests.Editor.asmdef`
- Delete: `Packages/InventorySystem/Tests/Editor/Mocks/MockInventoryItemConstraint.cs` (+ `.meta`), and `Mocks.meta` if the folder empties
- Modify: `Packages/InventorySystem/Tests/Editor/UnitTests/Game/InventorySystem/InventoryTest.cs`

**Interfaces:**
- Consumes: `Inventory<T>` (Task 5), Moq (Task 12).
- Produces: nothing later tasks depend on.

`IInventoryItemConstraint.ApplyTo(int)` must both record its argument *and* return it unchanged — the inventory stores whatever comes back, so a mock that returns `default` would zero every value.

- [ ] **Step 1: Wire the assembly**

Same `precompiledReferences` block as Task 12 Step 3.

- [ ] **Step 2: Rewrite the one test that uses it**

`ChangingTheValueOfAnItemShouldUseTheDefinedConstaintOnThatItem` becomes:

```csharp
        [Test]
        public void ChangingTheValueOfAnItemShouldUseTheDefinedConstaintOnThatItem()
        {
            var constraint = new Mock<IInventoryItemConstraint>();
            constraint.Setup(c => c.ApplyTo(It.IsAny<int>())).Returns<int>(value => value);

            inventory.SetConstraint(itemA, constraint.Object);

            inventory.SetNumberOf(itemA, 5);
            constraint.Verify(c => c.ApplyTo(5), Times.Once);

            inventory.Increase(itemA, 3);
            constraint.Verify(c => c.ApplyTo(5 + 3), Times.Once);

            inventory.Decrease(itemA, 1);
            constraint.Verify(c => c.ApplyTo(5 + 3 - 1), Times.Once);
        }
```

`Returns<int>(value => value)` is the pass-through the old double did implicitly. Without it every assertion downstream reads `0`.

Add `using Moq;` and delete the now-unused `using Arman.Mocks.Game.InventorySystem;`.

- [ ] **Step 3: Delete the double**

```bash
cd Packages/InventorySystem/Tests/Editor
git rm Mocks/MockInventoryItemConstraint.cs Mocks/MockInventoryItemConstraint.cs.meta
ls Mocks 2>/dev/null || git rm Mocks.meta
```

- [ ] **Step 4: Run this package's tests and commit**

```bash
unity command run_tests --mode EditMode --filter Arman.InventorySystem
git add -A
git commit -m "test(inventory-system): replace MockInventoryItemConstraint with Moq"
```

---

### Task 18: EventManagement — `ListenerMock` → Moq, `EventMock` → `FakeGameEvent`

**Files:**
- Modify: `Packages/EventManagement/Tests/Editor/Arman.EventManagement.Editor.Tests.asmdef`
- Modify: `Packages/EventManagement/Tests/Editor/UnitTests/Foundation/EventManagement/EventManagerTest.cs`

**Interfaces:**
- Consumes: `EventManager` (Task 4), Moq (Task 12).
- Produces: nothing later tasks depend on.

`IGameEvent` is an empty marker interface — `EventMock` has nothing to verify and stays a fake, renamed. `ListenerMock` only ever records the event it was handed, which is argument capture, so it becomes a mock.

**Note:** this assembly's asmdef references `UnityEditor.TestRunner` but **not** `UnityEngine.TestRunner`, unlike its siblings. Leave that alone — it compiles today and fixing it is unrelated.

- [ ] **Step 1: Wire the assembly**

Same `precompiledReferences` block as Task 12 Step 3.

- [ ] **Step 2: Rewrite the fixture**

```csharp
using Moq;
using NUnit.Framework;
using Arman.EventManagement;

namespace Arman.EventManagement.Tests
{
    public class EventManagerTest
    {
        class FakeGameEvent : IGameEvent
        {

        }

        IEventManager manager;

        Mock<IEventListener> listener1;
        Mock<IEventListener> listener2;

        [SetUp]
        public void Setup()
        {
            manager = new EventManager();

            listener1 = new Mock<IEventListener>();
            listener2 = new Mock<IEventListener>();
        }

        [Test]
        public void RegisteringListenrerShouldAddThemToManager()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            Assert.That(manager.Has(listener1.Object));
            Assert.That(manager.Has(listener2.Object));
        }

        [Test]
        public void UnregisteringListenrerShouldRemoveThemFromManager()
        {
            manager.Register(listener1.Object);

            manager.UnRegister(listener1.Object);

            Assert.That(manager.Has(listener1.Object), Is.False);
        }

        [Test]
        public void PropagatingAnEventShouldNotifyRegisteredListeners()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            IGameEvent evt = new FakeGameEvent();
            manager.Propagate(evt, this);

            listener1.Verify(l => l.OnEvent(evt, this), Times.Once);
            listener2.Verify(l => l.OnEvent(evt, this), Times.Once);
        }

        [Test]
        public void PropagatingAnEventShouldNotNotifyUnRegisteredListeners()
        {
            manager.Register(listener1.Object);
            manager.UnRegister(listener1.Object);

            IGameEvent evt = new FakeGameEvent();
            manager.Propagate(evt, this);
            manager.Propagate(evt, this);

            listener1.Verify(l => l.OnEvent(It.IsAny<IGameEvent>(), It.IsAny<object>()), Times.Never);
        }

        [Test]
        public void ClearingShouldRemoveAllListeners()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            manager.Clear();

            Assert.That(manager.Has(listener1.Object), Is.False);
            Assert.That(manager.Has(listener2.Object), Is.False);
        }
    }
}
```

- [ ] **Step 3: Run this package's tests and commit**

```bash
unity command run_tests --mode EditMode --filter Arman.EventManagement
git add -A
git commit -m "test(event-management): Moq for the listener, a named fake for the event"
```

---

### Task 19: ObjectPooling — `MockObject` → `FakePoolable`, no Moq

The one package where the answer is "this was never a mock". The pool constructs its own objects in `CreateObject()` and the tests assert identity across acquire/release, so there is nothing to inject a mock into. Rename it honestly and move on. This asmdef gets **no** Moq reference.

**Files:**
- Modify: `Packages/ObjectPooling/Tests/Editor/UnitTests/ObjectPooling/ObjectPoolTest.cs`

**Interfaces:**
- Consumes: `ObjectPool<T>` (Task 6).
- Produces: nothing later tasks depend on.

- [ ] **Step 1: Rename the two types in place**

In `ObjectPoolTest.cs`: `MockObject` → `FakePoolable`, and `TestableBasicObjectPool` → `TestableObjectPool` (left over from Task 6). Use `mcp__sharplens__rename_symbol` for both so every usage in the file follows.

The class bodies are unchanged. After the rename the declarations read:

```csharp
    public class FakePoolable : IPoolable
    {
        public int id;

        public bool isActive = false;

        public bool onAcquiredIsCalled = false;
        public bool onReleaseIsCalled = false;

        public FakePoolable(int id)
        {
            this.id = id;
        }

        public void OnAcquired()
        {
            onAcquiredIsCalled = true;
        }

        public void OnReleased()
        {
            onReleaseIsCalled = true;
        }

        public void SetActive(bool isActive)
        {
            this.isActive = isActive;
        }
    }

    public class TestableObjectPool : ObjectPool<FakePoolable>
    {

        public bool createMethodIsCalled = false;


        protected override FakePoolable CreateObject()
        {
            this.createMethodIsCalled = true;
            return new FakePoolable(1);
        }

        protected override void DeactivateObject(FakePoolable obj)
        {
            obj.SetActive(false);
        }


        protected override void ActivateObject(FakePoolable obj)
        {
            obj.SetActive(true);
        }
    }
```

- [ ] **Step 2: Run this package's tests and commit**

```bash
unity command run_tests --mode EditMode --filter Arman.ObjectPooling
git add -A
git commit -m "test(object-pooling): rename MockObject to FakePoolable, which is what it is"
```

---

### Task 20: Document the convention and verify everything

**Files:**
- Modify: `.agents/AGENTS.md`

**Interfaces:**
- Consumes: everything above.
- Produces: the written convention.

- [ ] **Step 1: Document the test-double convention**

In `.agents/AGENTS.md`, under the C# coding style section, add:

```markdown
### Test doubles

Two kinds, named for what they are:

* **Mocks** — the assertion *is* the interaction: a call happened, with these arguments, this many
  times, in this order. Use **Moq**, never a hand-written class. Moq arrives as
  [`nuget.moq`](https://docs.unity3d.com/Packages/nuget.moq@2.0/manual/index.html) `2.0.1`
  (Moq 4.18.2), declared in `Packages/manifest.json` and **never** in a package's `package.json` —
  a test-only dependency must not follow the package to a consumer's player build.

  Every test assembly here sets `"overrideReferences": true`, so a Moq-using assembly must name all
  three DLLs explicitly:

  ```json
  "precompiledReferences": [
      "nunit.framework.dll",
      "Moq.dll",
      "System.Runtime.CompilerServices.Unsafe.dll",
      "System.Threading.Tasks.Extensions.dll"
  ],
  ```

* **Fakes** — a small working implementation, asserted on as a value or passed around by identity.
  Hand-written, named `Fake<Thing>`. Prefer a fake whenever one will do.

Three cases that must stay fakes, as a guide to the boundary: a double dispatched on its **concrete
type** (`ShopCenter.PackagesOfType<T>()` — a Moq proxy's type is generated); a double the subject
**constructs itself** (`ObjectPool<T>.CreateObject()` — nothing to inject); and a double of an
**empty marker interface** (`IGameEvent` — nothing to verify).
```

- [ ] **Step 2: Note the renamed implementations**

In the same file, in the package-anatomy or naming area, add:

```markdown
**A package's principal implementation is named after its interface, without the `I`** — `UpdateManager`
implements `IUpdateManager`, `ShopCenter` implements `IShopCenter`. The old `Basic` prefix was dropped
on 2026-09-05; it distinguished each type from nothing, since each interface had exactly one
implementation. See [`docs/specs/2026-09-05-basic-rename-and-moq-design.md`](../docs/specs/2026-09-05-basic-rename-and-moq-design.md).
```

- [ ] **Step 3: Confirm no hand-written mock survives**

```bash
grep -rn --include=*.cs -oE '(class|struct)[[:space:]]+[A-Za-z0-9_]*(Mock|Stub|Dummy|Spy)[A-Za-z0-9_]*' Packages/
```

Expected: no hits. Every remaining double is either a `Fake*` or a `Mock<T>` from Moq.

- [ ] **Step 4: Confirm no `Basic` implementation survives**

```bash
grep -rn --include=*.cs -oE '(class|interface|struct)[[:space:]]+[A-Za-z0-9_]*Basic[A-Za-z0-9_]*' Packages/
```

Expected: exactly one hit — `JsonBasic` in `Packages/PackageBasics/Runtime/ThirdParties/NiceJson.cs`, which is vendored third-party code.

- [ ] **Step 5: Full verification**

```bash
node Tools/upm-release.mjs validate
node Tools/changelog-check.mjs --base dev --head HEAD
unity command run_tests --mode EditMode
```

Expected: `validate` exit 0, `changelog-check` exit 0, suite at baseline counts with every test passing.

Also confirm no version was bumped and no `## [Unreleased]` heading was left empty:

```bash
git diff dev...HEAD -- '**/package.json'
```

Expected: **empty**. Part A adds CHANGELOG entries only; bumping is a separate, deliberate act.

- [ ] **Step 6: Commit and open the pull request**

```bash
git add -A
git commit -m "docs(agents): record the test-double and implementation-naming conventions"
git push -u origin refactor/drop-basic-prefix-and-moq
gh pr create --base dev --title "Drop the Basic prefix, and use Moq where the test checks an interaction"
```

`gh pr create` targets `dev` by default in this repo, but pass `--base dev` explicitly anyway. **Never** target `master` — that is the release path.

---

## Self-review notes

**Spec coverage.** Spec §3.1 twelve renames → Tasks 1–10. §3.2 `.meta` handling → every rename task's move step, plus Task 11 Step 2. §3.3 CHANGELOG entries → each rename task, verified in Task 11 Step 3. §3.4 both risks → Task 6's note (`UnityEngine.Pool`) and Task 10 Steps 1/3 (GUID). §4.1 dependency wiring → Task 12, including the duplicate-assembly contingency. §4.2 eight Moq conversions → Tasks 13 (1), 14 (3), 15 (1), 16 (1), 17 (1), 18 (1). §4.3 five fakes → Tasks 15 (three shop packages), 18 (`FakeGameEvent`), 19 (`FakePoolable`). §4.4 no bumps → Task 20 Step 5. §5 order → task order. §7 verification → Task 20 Step 5.

**Known gap, accepted.** `ConfigurationManagement`'s `FakeConfigurer<T>` and `FakeMultiConfigurerAB` are declared out of scope by spec §2.2 and have no task. Its asmdef gains no Moq reference.

**Type consistency.** `Container<T>` (Task 1) is consumed by Task 7. `CacheableEntity<T>` (Task 2) is constructed as `CacheableEntity<ICache>` in Task 16. `ObjectPool<T>` (Task 6) is subclassed as `TestableObjectPool` in Task 19. `PersistentDataManagerTestContext` (Task 7) has its serializer fields retyped in Task 14. The `precompiledReferences` block is identical in Tasks 12, 14, 15, 16, 17, 18 and quoted in full in Task 20.
