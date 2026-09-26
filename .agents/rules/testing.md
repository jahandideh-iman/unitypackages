# Testing

Running the Unity test suites, and how tests in this repo are written.

## Test Commands

Unity tests run through the official [Unity CLI](https://unity.com/blog/meet-the-unity-cli) (the standalone `unity` binary — `unity doctor` shows install/auth state; not to be confused with `Unity.exe` batchmode, which it wraps). Ensure the Editor named in `ProjectSettings/ProjectVersion.txt` is installed and registered (`unity editors`). There are two ways to run tests, pick based on whether an interactive Editor is already open on this project:

- **No Editor open (CI, fresh checkout) — spawns its own batch instance:**

  ```powershell
  unity test --mode EditMode --output Library/editmode-results.xml
  unity test --mode PlayMode --output Library/playmode-results.xml
  ```

  **Fails with "another Unity instance is running with this project open" if the Editor is already open** — Unity refuses to open the same project twice. Use the next option instead.

  `unity test` auto-detects the project (current directory) and editor version (`ProjectVersion.txt`); pass `--editor-version`/`-e <path>` to override, `--allow-install` to fetch a missing editor version, and `--timeout <seconds>` to cap a hung run. Add `--json` for machine-parseable output.

  **Exit codes (CLI 1.0.0-beta.3):** `0` success, **`8` tests ran and failed**, **`6` the run never produced results** (compiler errors, a missing `--execute-method` target, a dead Editor). Check that distinction before treating a nonzero exit as "couldn't run" — `6` genuinely means "couldn't run," `8` means the suite is red.

  For CI, `--report-format nunit,junit --junit-output <path>` emits a JUnit report alongside the NUnit one, which the `report` job turns into PR annotations. No external XSLT step is needed.

- **Editor already open (the common case while developing) — runs in the live instance, no second process, faster:**
  ```powershell
  unity command run_tests --mode EditMode
  unity command run_tests --mode PlayMode
  ```
  Returns structured JSON with per-test results inline (`Summary.{Total,Passed,Failed}`, `Results[].{FullName,Status,Duration}`) — no XML file to parse. `--filter`/`--filter_type` narrow to specific tests; see `unity command` (no args) for the full parameter list.

### Test doubles — Moq for interactions, hand-written fakes for state

The repo uses [`nuget.moq`](https://docs.unity3d.com/Packages/nuget.moq@2.0/manual/index.html) 2.0.1, declared in `Packages/manifest.json`. **Pick by what the test asserts, not by habit:**

- **The assertion _is_ the interaction** — call counts, captured arguments, ordering, "was this collaborator used at all" → use `Mock<T>` and `Verify`. A hand-written class that exists only to increment a counter is re-implementing `Times.Once`.
- **The assertion is state or identity** — the object is compared, applied, returned, or dispatched on → write a small `Fake*` class. Name it `Fake<Thing>`, not `<Thing>Mock`.

Two cases genuinely need a real type, and both are in the tree as examples:

- `FakeShopPackage` / `FakeShopPackageA` / `FakeShopPackageB` — `ShopCenter.PackagesOfType<T>()` and `AssignPurchaseHandler<T>()` dispatch on the **concrete** type argument, and a Moq proxy's runtime type is generated, so it cannot express them.
- `FakePoolable` — `ObjectPool<T>` constructs its own instances in `CreateObject()`, so there is nothing to hand a proxy to.

**Wiring a test assembly for Moq.** Every test asmdef sets `"overrideReferences": true`, so Moq and the two support assemblies it ships have to be listed explicitly — adding the package alone is not enough:

```json
"precompiledReferences": [
    "nunit.framework.dll",
    "Moq.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Threading.Tasks.Extensions.dll"
],
```

Do **not** fix a duplicate-assembly error by turning `overrideReferences` off — that silently widens the assembly's reference set.

**Moq is loose by default:** an unconfigured method returns `default`. Any collaborator with a fluent interface (`IPersistentDataWrapper.WriteInt` and friends return the wrapper) or a meaningful `bool` (`HasReadableStreamFor`, `HasKey`) needs an explicit `Setup`, or the code under test dereferences a null it never saw before. See `PersistentDataManagerTestContext` for the shared factory helpers this repo uses.
