
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arman.Foundation.ShopManagement.Core
{
    public class BasicShopCenter : IShopCenter
    {
        class PurchaseHandlingData
        {
            public Type targetPackageType;

            public IPurchaseHandler purchaseHandler;

            public PurchaseHandlingData(Type packageType, IPurchaseHandler purchaseHandler)
            {
                this.targetPackageType = packageType;
                this.purchaseHandler = purchaseHandler;
            }

            public bool IsAppliedTo(IShopPackage shopPackage)
            {
                return 
                    shopPackage.GetType().IsSubclassOf(targetPackageType) ||
                    shopPackage.GetType().Equals(targetPackageType);
            }
        }

        List<IShopPackage> packages = new List<IShopPackage>();
        List<PurchaseHandlingData> purchaseHandlingDataList = new List<PurchaseHandlingData>();

        Action<IShopPackage,IPurchaseSuccessResult> globalOnPurchaseSucceeded = delegate { };
        Action<IShopPackage,IPurchaseFailureResult> globalOnPurchaseFailed = delegate { };

        public void AddPackage(IShopPackage package)
        {
            packages.Add(package);
        }

        public void RemovePackage(IShopPackage package)
        {
            packages.Remove(package);
        }

        public void AssignPurchaseHandler<T>(IPurchaseHandler purchaseHandler) where T : IShopPackage
        {
            purchaseHandlingDataList.Add(new PurchaseHandlingData(typeof(T), purchaseHandler));
        }

        public void Purchase(IShopPackage package, Action<IPurchaseSuccessResult> onSuccess, Action<IPurchaseFailureResult> onFailure)
        {
            var purchaseHandler = FindPurchaseHandlerFor(package);

            purchaseHandler.Purchase(
                package,
                onSuccess: (result) => HandlePurchaseSuccess(package, onSuccess, result), 
                onFailure: (result) => HandlePurchaseFailure(package, onFailure, result));
        }

        private void HandlePurchaseSuccess(IShopPackage package, Action<IPurchaseSuccessResult> onSuccess, IPurchaseSuccessResult result)
        {
            ApplyPackage(package); 
            onSuccess(result);
            globalOnPurchaseSucceeded.Invoke(package, result);
        }

        private void HandlePurchaseFailure(IShopPackage package, Action<IPurchaseFailureResult> onFailure, IPurchaseFailureResult result)
        {
            onFailure(result);
            globalOnPurchaseFailed.Invoke(package, result);
        }

        private void ApplyPackage(IShopPackage package)
        {
            package.Apply();
        }

        public void SetPurchaseSuccessCallback(Action<IShopPackage, IPurchaseSuccessResult> onPurchaseSucceeded)
        {
            this.globalOnPurchaseSucceeded = onPurchaseSucceeded;
        }

        public void SetPurchaseFailureCallback(Action<IShopPackage, IPurchaseFailureResult> onPurchaseFailed)
        {
            this.globalOnPurchaseFailed = onPurchaseFailed;
        }

        public ICollection<IShopPackage> Packages()
        {
            return packages;
        }

        public ICollection<T> PackagesOfType<T>() where T : IShopPackage
        {
            return packages.Where(p => p is T).Cast<T>().ToList();
        }


        private IPurchaseHandler FindPurchaseHandlerFor(IShopPackage package)
        {
            foreach (var data in purchaseHandlingDataList)
                if (data.IsAppliedTo(package))
                    return data.purchaseHandler;

            return null;
        }

    }
}