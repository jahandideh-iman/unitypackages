using Arman.Foundation.ShopManagement.Core;
using System;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class PurchaseHandlerMock : IPurchaseHandler
    {
        public IShopPackage givenShopPackage;

        bool shoudSucceed = false;

        public void Clear()
        {
            givenShopPackage = null;
        }

        public void Purchase(IShopPackage shopPackage, Action<IPurchaseSuccessResult> onSuccess, Action<IPurchaseFailureResult> onFailure)
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