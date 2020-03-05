
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arman.Foundation.ShopManagement.Core
{
    public class BasicShopCenter : ShopCenter
    {
        class PurchaseHandlingData
        {
            public Type targetPackageType;

            public PurchaseHandler purchaseHandler;

            public PurchaseHandlingData(Type packageType, PurchaseHandler purchaseHandler)
            {
                this.targetPackageType = packageType;
                this.purchaseHandler = purchaseHandler;
            }

            public bool IsAppliedTo(ShopPackage shopPackage)
            {
                return 
                    shopPackage.GetType().IsSubclassOf(targetPackageType) ||
                    shopPackage.GetType().Equals(targetPackageType);
            }
        }

        List<ShopPackage> packages = new List<ShopPackage>();
        List<PurchaseHandlingData> purchaseHandlingDataList = new List<PurchaseHandlingData>();

        Action<ShopPackage,PurchaseSuccessResult> globalOnPurchaseSucceeded = delegate { };
        Action<ShopPackage,PurchaseFailureResult> globalOnPurchaseFailed = delegate { };

        public void AddPackage(ShopPackage package)
        {
            packages.Add(package);
        }

        public void RemovePackage(ShopPackage package)
        {
            packages.Remove(package);
        }

        public void AssignPurchaseHandler<T>(PurchaseHandler purchaseHandler) where T : ShopPackage
        {
            purchaseHandlingDataList.Add(new PurchaseHandlingData(typeof(T), purchaseHandler));
        }

        public void Purchase(ShopPackage package, Action<PurchaseSuccessResult> onSuccess, Action<PurchaseFailureResult> onFailure)
        {
            var purchaseHandler = FindPurchaseHandlerFor(package);

            purchaseHandler.Purchase(
                package,
                onSuccess: (result) => HandlePurchaseSuccess(package, onSuccess, result), 
                onFailure: (result) => HandlePurchaseFailure(package, onFailure, result));
        }

        private void HandlePurchaseSuccess(ShopPackage package, Action<PurchaseSuccessResult> onSuccess, PurchaseSuccessResult result)
        {
            ApplyPackage(package); 
            onSuccess(result);
            globalOnPurchaseSucceeded.Invoke(package, result);
        }

        private void HandlePurchaseFailure(ShopPackage package, Action<PurchaseFailureResult> onFailure, PurchaseFailureResult result)
        {
            onFailure(result);
            globalOnPurchaseFailed.Invoke(package, result);
        }

        private void ApplyPackage(ShopPackage package)
        {
            package.Apply();
        }

        public void SetPurchaseSuccessCallback(Action<ShopPackage, PurchaseSuccessResult> onPurchaseSucceeded)
        {
            this.globalOnPurchaseSucceeded = onPurchaseSucceeded;
        }

        public void SetPurchaseFailureCallback(Action<ShopPackage, PurchaseFailureResult> onPurchaseFailed)
        {
            this.globalOnPurchaseFailed = onPurchaseFailed;
        }

        public ICollection<ShopPackage> Packages()
        {
            return packages;
        }

        public ICollection<T> PackagesOfType<T>() where T : ShopPackage
        {
            return packages.Where(p => p is T).Cast<T>().ToList();
        }


        private PurchaseHandler FindPurchaseHandlerFor(ShopPackage package)
        {
            foreach (var data in purchaseHandlingDataList)
                if (data.IsAppliedTo(package))
                    return data.purchaseHandler;

            return null;
        }

    }
}