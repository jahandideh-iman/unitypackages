using System;
using System.Collections.Generic;
using Arman.ShopManagement;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class FakePurchaseSuccessResult : IPurchaseSuccessResult { }

    public class FakePurchaseFailureResult : IPurchaseFailureResult { }

    // Answers every purchase at once with the result it was given, and keeps the
    // packages it was asked to purchase.
    public class FakePurchaseHandler : IPurchaseHandler
    {
        public readonly List<IShopPackage> PurchasedPackages = new List<IShopPackage>();

        readonly IPurchaseSuccessResult? _successResult;
        readonly IPurchaseFailureResult? _failureResult;

        FakePurchaseHandler(
            IPurchaseSuccessResult? successResult,
            IPurchaseFailureResult? failureResult
        )
        {
            _successResult = successResult;
            _failureResult = failureResult;
        }

        public static FakePurchaseHandler Succeeding(IPurchaseSuccessResult result)
        {
            return new FakePurchaseHandler(result, null);
        }

        public static FakePurchaseHandler Failing(IPurchaseFailureResult result)
        {
            return new FakePurchaseHandler(null, result);
        }

        public void Purchase(
            IShopPackage shopPackage,
            Action<IPurchaseSuccessResult> onSuccess,
            Action<IPurchaseFailureResult> onFailure
        )
        {
            PurchasedPackages.Add(shopPackage);

            if (_successResult != null)
                onSuccess(_successResult);
            else
                onFailure(_failureResult!);
        }
    }
}
