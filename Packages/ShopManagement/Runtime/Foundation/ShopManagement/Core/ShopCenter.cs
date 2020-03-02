
using System;
using System.Collections.Generic;

namespace Arman.Foundation.ShopManagement.Core
{
    public interface PurchaseSuccessResult { }
    public interface PurchaseFailureResult { }

    public interface ShopCenter
    {
        void AddPackage(ShopPackage package);

        void AssignPurchaseHandler<T>(PurchaseHandler purchaseHandler) where T : ShopPackage;

        void Purchase(ShopPackage package, Action<PurchaseSuccessResult> onSuccess, Action<PurchaseFailureResult> onFailure);

        ICollection<ShopPackage> Packages();
        ICollection<T> PackagesOfType<T>() where T : ShopPackage;
    }
}