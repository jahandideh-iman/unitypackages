

using System;

namespace Arman.Foundation.ShopManagement.Core
{
    public interface PurchaseHandler
    {
        void Purchase(ShopPackage shopPackage, Action<PurchaseSuccessResult> onSuccess, Action<PurchaseFailureResult> onFailure);
    }
}