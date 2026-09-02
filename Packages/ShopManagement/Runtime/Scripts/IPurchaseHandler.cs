

using System;

namespace Arman.ShopManagement
{
    public interface IPurchaseHandler
    {
        void Purchase(IShopPackage shopPackage, Action<IPurchaseSuccessResult> onSuccess, Action<IPurchaseFailureResult> onFailure);
    }
}