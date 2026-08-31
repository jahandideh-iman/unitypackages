# Inventory System

Tracks how many of each item the player holds. An `IInventory<T>` maps items to integer counts,
clamps each count through an optional `IInventoryItemConstraint`, and raises callbacks when a count
changes.

It is a quantity ledger, not a slot- or grid-based container: there is no notion of stacks,
positions, or equipment slots.

## What it provides

Namespace `Arman.Game.InventorySystem.Core`:

| Type | Purpose |
|---|---|
| `IInventoryItem` | Marker contract for anything an inventory can hold. |
| `IInventory<T>` | `SetNumberOf`, `Increase`, `Decrease`, `NumberOf`, `Has`, `Items`, `SetConstraint`, and the two callback setters. |
| `BasicInventory<T>` | The in-memory implementation. |
| `IInventoryItemConstraint` | `int ApplyTo(int value)` — clamps a proposed count. |
| `MinMaxInventoryItemConstraint` | Clamps a count between a minimum and a maximum. |
| `OnItemNumberChanged<T>` | `delegate void (T item, int value)`. |

## Usage

```csharp
using Arman.Game.InventorySystem.Core;

public class Currency : IInventoryItem
{
    public string Name { get; }
    public Currency(string name) => Name = name;
}

var coins = new Currency("Coins");

var inventory = new BasicInventory<Currency>();
inventory.SetConstraint(coins, new MinMaxInventoryItemConstraint(0, 999));

inventory.SetNumberOf(coins, 10);
inventory.Increase(coins, 5);      // 15
inventory.Decrease(coins, 20);     // clamped to 0, not -5

int amount = inventory.NumberOf(coins);
bool canAfford = inventory.Has(coins, 100);
```

Reacting to changes — globally, or for one item:

```csharp
inventory.SetGlobalOnValueChangeCallback(
    (item, value) => Debug.Log($"{item.Name} is now {value}"));

inventory.SetSpecificOnValueChangeCallback(
    coins, (item, value) => coinLabel.text = value.ToString());
```

Enumerating what is held:

```csharp
foreach (Currency item in inventory.Items())
    Debug.Log($"{item.Name}: {inventory.NumberOf(item)}");
```

## Things to know

- **`IInventory<T>` is generic over the item type**, constrained to `IInventoryItem`, which is an
  empty marker interface. One inventory instance holds one kind of item; use separate inventories for
  unrelated item families.
- **`SetNumberOf` must come first.** `Increase`, `Decrease`, `NumberOf` and `Has` index the backing
  dictionary directly and throw `KeyNotFoundException` for an item that was never given a count.
  Seed each item with `SetNumberOf(item, 0)` before using the others.
- **Constraints clamp, they do not reject.** `MinMaxInventoryItemConstraint` returns the clamped
  value, so `Decrease` past the minimum silently settles at the minimum rather than failing.
- **Constraints are per item**, set with `SetConstraint`. An item with no constraint is unbounded.
- **`SetGlobalOnValueChangeCallback` and `SetSpecificOnValueChangeCallback` set, not add.** Calling
  either twice replaces the previous callback; they are not multicast subscriptions. Both fire on
  every `SetNumberOf`, including one that clamps to an unchanged value.
- **`Items()` allocates.** It copies the key set into a new `List<T>` on each call — keep it out of
  per-frame code.
