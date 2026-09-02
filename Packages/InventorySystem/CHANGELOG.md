# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Flattened `Runtime/Scripts` into a single folder and renamed the runtime namespace from `Arman.Game.InventorySystem.Core` to `Arman.InventorySystem`.

## [0.1.0] - 2026-08-30

First release of *Inventory System*.

### Added

- `IInventoryItem`, `IInventory<T>` and `IInventoryItemConstraint` — the core contracts.
- `BasicInventory<T>`, with `SetNumberOf`, `Increase`, `Decrease`, `NumberOf`, `Has`, `Items` and `SetConstraint`.
- `MinMaxInventoryItemConstraint`, which clamps an item count between a minimum and a maximum.
- `OnItemNumberChanged<T>` change callbacks, registrable globally or per item.
