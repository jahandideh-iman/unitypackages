

using System;

namespace Arman.Foundation.ShopManagement.Core
{
    public interface IPurchaseHandler
    {
        void Purchase(IShopPackage shopPackage, Action<IPurchaseSuccessResult> onSuccess, Action<IPurchaseFailureResult> onFailure);
    }
}