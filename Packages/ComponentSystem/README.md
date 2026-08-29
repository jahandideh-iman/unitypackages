# Component System

Component-based entity system providing type-safe component management through **IEntity** interface. Features include adding components via `AddComponent/AddComponents`, retrieving components by type using `GetComponent<T>()`, and enumerating all components. Implements virtual `OnComponentAdded` hook for notifications.

## Core Interfaces

- **IEntity** - Base entity interface with component management
- **ISpecializedEntity** - Specialized entity variant for specific use cases

## Usage

```csharp
// Create an entity
var entity = new BasicEntity();

// Add a single component
entity.AddComponent(new HealthComponent());

// Add multiple components at once
entity.AddComponents(
    new PositionComponent(),
    new VelocityComponent()
);

// Retrieve a component by type
var health = entity.GetComponent<HealthComponent>();

// Enumerate all components
foreach (var comp in entity.AllComponents())
{
    // Process component
}
```

## Implementation Details

- Components stored internally as both `List<T>` and cached `T[]` for performance
- Virtual `OnComponentAdded` hook enables notification when new components are added
- Generic type-safe component retrieval without LINQ overhead
