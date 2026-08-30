# Shop Management

Storefront and purchasing logic for an in-game shop. A shop centre holds the packages on offer,
routes a purchase to whichever handler is registered for that package's type, and applies the package
once the handler reports success.

The package deliberately knows nothing about payment. `IPurchaseHandler` is the seam where you plug
in Unity IAP, an in-game currency wallet, an ad-reward flow, or a test double.

## What it provides

Everything lives in the `Arman.Foundation.ShopManagement.Core` namespace.

| Type | Purpose |
|---|---|
| `IShopPackage` | The unit of purchase. A single `Apply()` — what the player gets. |
| `CompositeShopPackage` | An `IShopPackage` that holds others and applies them all; use it for bundles. |
| `IPurchaseHandler` | `Purchase(package, onSuccess, onFailure)` — performs the transaction. |
| `IPurchaseSuccessResult` / `IPurchaseFailureResult` | Marker interfaces for handler-specific result payloads. |
| `IShopCenter` | The storefront contract. |
| `BasicShopCenter` | The implementation. |

`IShopCenter` offers `AddPackage`, `RemovePackage`, `Packages()`, `PackagesOfType<T>()`,
`AssignPurchaseHandler<T>`, `Purchase`, `SetPurchaseSuccessCallback` and
`SetPurchaseFailureCallback`.

## Usage

Define what a package grants, and how it is paid for:

```csharp
using Arman.Foundation.ShopManagement.Core;

public class CoinPackage : IShopPackage
{
    private readonly int _amount;
    public CoinPackage(int amount) => _amount = amount;

    public void Apply() => Wallet.Add(_amount);
}

public class CurrencyPurchaseHandler : IPurchaseHandler
{
    public void Purchase(
        IShopPackage package,
        Action<IPurchaseSuccessResult> onSuccess,
        Action<IPurchaseFailureResult> onFailure)
    {
        if (Wallet.TrySpend(PriceOf(package)))
            onSuccess(new CurrencyPurchaseSuccess());
        else
            onFailure(new InsufficientFunds());
    }
}
```

Wire up the shop:

```csharp
var shop = new BasicShopCenter();

shop.AddPackage(new CoinPackage(100));
shop.AssignPurchaseHandler<CoinPackage>(new CurrencyPurchaseHandler());

shop.SetPurchaseSuccessCallback((package, result) => analytics.LogPurchase(package));
shop.SetPurchaseFailureCallback((package, result) => ui.ShowError(result));
```

Run a purchase. `Apply()` is called for you before `onSuccess`:

```csharp
shop.Purchase(
    package,
    onSuccess: result => ui.ShowReceipt(result),
    onFailure: result => ui.ShowError(result));
```

Bundles are just packages made of packages:

```csharp
var starterPack = new CompositeShopPackage();
starterPack.Add(new CoinPackage(500));
starterPack.Add(new RemoveAdsPackage());

shop.AddPackage(starterPack);
shop.AssignPurchaseHandler<CompositeShopPackage>(new IAPPurchaseHandler());
```

Filtering the storefront for display:

```csharp
foreach (CoinPackage coins in shop.PackagesOfType<CoinPackage>())
    ui.AddTile(coins);
```

## Things to know

- **Handlers are matched by package type**, exactly or by subclass, in the order they were assigned.
  The first match wins.
- **A package with no assigned handler throws.** `FindPurchaseHandlerFor` returns `null` and
  `Purchase` dereferences it — assign a handler for every package type you add.
- **`Apply()` runs on success, before your `onSuccess` callback**, and then the global success
  callback fires. On failure nothing is applied.
- **The global callbacks are set, not added.** Calling `SetPurchaseSuccessCallback` twice replaces
  the previous delegate.
- **`AddPackage` does not deduplicate**, and `Packages()` returns the live internal list — copy it
  before mutating the shop while iterating.
