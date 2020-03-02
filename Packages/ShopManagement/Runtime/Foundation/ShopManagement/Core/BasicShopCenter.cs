
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

        public void AddPackage(ShopPackage package)
        {
            packages.Add(package);
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
                (result) => { ApplyPackage(package); onSuccess(result); }, 
                onFailure);
        }

        private void ApplyPackage(ShopPackage package)
        {
            package.Apply();
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