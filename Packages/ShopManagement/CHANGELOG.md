# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- A `csc.rsp` response file carrying `-nullable:enable` next to every assembly definition, so nullable reference type annotations are enforced when the package is compiled.

### Changed

- Flattened `Runtime/Scripts` into a single folder and renamed the runtime namespace from `Arman.Foundation.ShopManagement.Core` to `Arman.ShopManagement`.

## [0.1.0] - 2026-08-30

First release of *Shop Management*.

### Added

- `IShopPackage`, the unit of purchase, and `CompositeShopPackage`, which applies a group of packages as one.
- `IPurchaseHandler` together with the `IPurchaseSuccessResult` and `IPurchaseFailureResult` result markers.
- `IShopCenter` and `BasicShopCenter` — add and remove packages, query them with `Packages` and `PackagesOfType<T>`, assign a purchase handler per package type, and run a purchase that applies the package on success.
- Global purchase callbacks via `SetPurchaseSuccessCallback` and `SetPurchaseFailureCallback`.
