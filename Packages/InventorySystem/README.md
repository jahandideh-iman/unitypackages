# Inventory System

A simple inventory abstraction. An `IInventory` stores `IInventoryItem`s and enforces per-item count limits through `IInventoryItemConstraint`s (the built-in `MinMaxInventoryItemConstraint` caps items between a minimum and maximum).

## What it provides

- `IInventory` — add, consume, and query items; `HasRoomToAdd`.
- `IInventoryItem` / `ItemInventory` — the item contract and its string-identity implementation.
- `IInventoryItemConstraint` / `MinMaxInventoryItemConstraint` — count-limit constraints (including `MaxItemCountConstraint`).

## Usage

```csharp
using Arman.InventorySystem;

IInventory inventory = new BasicInventory();
inventory.AddInventoryItem(new ItemInventory("Coin"), new MinMaxInventoryItemConstraint(0, 10));

inventory.ConsumeItem("Coin", 2);
bool hasRoom = inventory.CanAddItem("Coin", 5);
```
