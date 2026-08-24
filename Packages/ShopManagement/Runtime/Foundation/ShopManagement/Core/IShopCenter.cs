
using System;
using System.Collections.Generic;

namespace Arman.Foundation.ShopManagement.Core
{
    public interface IPurchaseSuccessResult { }
    public interface IPurchaseFailureResult { }

    public interface IShopCenter
    {
        void AddPackage(IShopPackage package);

        void AssignPurchaseHandler<T>(IPurchaseHandler purchaseHandler) where T : IShopPackage;

        void Purchase(IShopPackage package, Action<IPurchaseSuccessResult> onSuccess, Action<IPurchaseFailureResult> onFailure);

        ICollection<IShopPackage> Packages();
        ICollection<T> PackagesOfType<T>() where T : IShopPackage;

        void SetPurchaseSuccessCallback(Action<IShopPackage, IPurchaseSuccessResult> onPurchaseSucceeded);
        void SetPurchaseFailureCallback(Action<IShopPackage, IPurchaseFailureResult> onPurchaseFailed);

        void RemovePackage(IShopPackage package);
    }
}