# Configuration Management

Abstraction for game configuration values. A `IConfigurationManager` aggregates one or more `IConfigurer`s and answers typed `GetConfig<T>(key)` lookups. The Unity sub-namespace backs a configurer from a `ScriptableObject` holding `Config` values, so data designers can author values in the Editor.

## What it provides

- `IConfigurationManager` / `IConfigurer` — the core contracts.
- `BasicConfigurationManager` (with `BasicDataValue<T>`) and `CompositeConfigurer` (combine configurers).
- `DynamicConfigurer` (with `DynamicValueProvider`) for runtime-computed values.
- Unity: `UnityConfigurationManager`, `ScriptableConfiguration`, and `AutoFillAssetArrayAttribute`.

## Usage

```csharp
using Arman.ConfigurationManagement;
using Arman.ConfigurationManagement.Unity;

IConfigurationManager configuration = new UnityConfigurationManager();
configuration.AddConfigurer(new ScriptableConfiguration(myConfigAsset));

int highScore = configuration.GetConfig<int>("HighScore");
```
