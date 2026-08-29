# Event Management

Pub-sub event management system that allows registration of event listeners, propagation of game events to all registered handlers, and listener list management (register/unregister/clear). Uses copy-on-write pattern for safe event propagation.

## Core Interfaces

- **IGameEvent** - Base interface for all game events
- **IEventManager** - Manages event listeners and propagation  
- **IEventListener** - Interface implemented by event handlers

## Usage

```csharp
// Register a listener
var listener = new MyEventHandler();
eventManager.Register(listener);

// Propagate an event to all listeners
eventManager.Propagate(myEvent, sender);

// Unregister or clear listeners
eventManager.UnRegister(listener);
eventManager.Clear();
```

## Implementation Details

- Copy-on-write pattern prevents mutation during callback execution
- Thread-safe event propagation without race conditions
