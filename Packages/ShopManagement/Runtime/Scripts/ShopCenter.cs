using System;
using System.Collections.Generic;
using System.Linq;

namespace Arman.ShopManagement
{
    public class ShopCenter : IShopCenter
    {
        class PurchaseHandlingData
        {
            public Type TargetPackageType;

            public IPurchaseHandler PurchaseHandler;

            public PurchaseHandlingData(Type packageType, IPurchaseHandler purchaseHandler)
            {
                this.TargetPackageType = packageType;
                this.PurchaseHandler = purchaseHandler;
            }

            public bool IsAppliedTo(IShopPackage shopPackage)
            {
                return shopPackage.GetType().IsSubclassOf(TargetPackageType)
                    || shopPackage.GetType().Equals(TargetPackageType);
            }
        }

        List<IShopPackage> _packages = new List<IShopPackage>();
        List<PurchaseHandlingData> _purchaseHandlingDataList = new List<PurchaseHandlingData>();

        Action<IShopPackage, IPurchaseSuccessResult> _globalOnPurchaseSucceeded = delegate { };
        Action<IShopPackage, IPurchaseFailureResult> _globalOnPurchaseFailed = delegate { };

        public void AddPackage(IShopPackage package)
        {
            _packages.Add(package);
        }

        public void RemovePackage(IShopPackage package)
        {
            _packages.Remove(package);
        }

        public void AssignPurchaseHandler<T>(IPurchaseHandler purchaseHandler)
            where T : IShopPackage
        {
            _purchaseHandlingDataList.Add(new PurchaseHandlingData(typeof(T), purchaseHandler));
        }

        public void Purchase(
            IShopPackage package,
            Action<IPurchaseSuccessResult> onSuccess,
            Action<IPurchaseFailureResult> onFailure
        )
        {
            var purchaseHandler = FindPurchaseHandlerFor(package);

            purchaseHandler.Purchase(
                package,
                onSuccess: (result) => HandlePurchaseSuccess(package, onSuccess, result),
                onFailure: (result) => HandlePurchaseFailure(package, onFailure, result)
            );
        }

        private void HandlePurchaseSuccess(
            IShopPackage package,
            Action<IPurchaseSuccessResult> onSuccess,
            IPurchaseSuccessResult result
        )
        {
            ApplyPackage(package);
            onSuccess(result);
            _globalOnPurchaseSucceeded.Invoke(package, result);
        }

        private void HandlePurchaseFailure(
            IShopPackage package,
            Action<IPurchaseFailureResult> onFailure,
            IPurchaseFailureResult result
        )
        {
            onFailure(result);
            _globalOnPurchaseFailed.Invoke(package, result);
        }

        private void ApplyPackage(IShopPackage package)
        {
            package.Apply();
        }

        public void SetPurchaseSuccessCallback(
            Action<IShopPackage, IPurchaseSuccessResult> onPurchaseSucceeded
        )
        {
            this._globalOnPurchaseSucceeded = onPurchaseSucceeded;
        }

        public void SetPurchaseFailureCallback(
            Action<IShopPackage, IPurchaseFailureResult> onPurchaseFailed
        )
        {
            this._globalOnPurchaseFailed = onPurchaseFailed;
        }

        public ICollection<IShopPackage> Packages()
        {
            return _packages;
        }

        public ICollection<T> PackagesOfType<T>()
            where T : IShopPackage
        {
            return _packages.Where(p => p is T).Cast<T>().ToList();
        }

        private IPurchaseHandler FindPurchaseHandlerFor(IShopPackage package)
        {
            foreach (var data in _purchaseHandlingDataList)
                if (data.IsAppliedTo(package))
                    return data.PurchaseHandler;

            return null;
        }
    }
}
