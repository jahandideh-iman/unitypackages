
using Arman.Foundation.Core.ServiceLocating;
using System;
using System.Collections.Generic;

namespace Arman.Foundation.ShopManagement.Core
{
    public interface PurchaseSuccessResult { }
    public interface PurchaseFailureResult { }

    public interface ShopCenter : Service
    {
        void AddPackage(ShopPackage package);

        void AssignPurchaseHandler<T>(PurchaseHandler purchaseHandler) where T : ShopPackage;

        void Purchase(ShopPackage package, Action<PurchaseSuccessResult> onSuccess, Action<PurchaseFailureResult> onFailure);

        ICollection<ShopPackage> Packages();
        ICollection<T> PackagesOfType<T>() where T : ShopPackage;

        void SetPurchaseSuccessCallback(Action<ShopPackage, PurchaseSuccessResult> onPurchaseSucceeded);
        void SetPurchaseFailureCallback(Action<ShopPackage, PurchaseFailureResult> onPurchaseFailed);

        void RemovePackage(ShopPackage package);
    }
}