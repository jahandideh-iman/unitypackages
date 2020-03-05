using Arman.Foundation.ShopManagement.Core;
using System;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class PurchaseHandlerMock : PurchaseHandler
    {
        public ShopPackage givenShopPackage;

        bool shoudSucceed = false;

        public void Clear()
        {
            givenShopPackage = null;
        }

        public void Purchase(ShopPackage shopPackage, Action<PurchaseSuccessResult> onSuccess, Action<PurchaseFailureResult> onFailure)
        {
            givenShopPackage = shopPackage;

            if (shoudSucceed)
                onSuccess(null);
            else
                onFailure(null);
        }

        public void ShouldSucceed(bool value)
        {
            shoudSucceed = value;
        }
    }
}