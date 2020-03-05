using Arman.Foundation.ShopManagement.Core;
using Arman.Mocks.Foundation.ShopManagement.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.ShopManagement.Core
{
    public class ShopPackageMockA : ShopPackageMock {}

    public class ShopPackageMockB : ShopPackageMock {}


    public class BasicShopCenterTest 
    {
        ShopCenter shopCenter;

        [SetUp]
        public void Setup()
        {
            shopCenter = new BasicShopCenter();
        }

        [Test]
        public void ShoudHaveTheAddedPackages()
        {
            var package1 = new ShopPackageMock();
            var package2 = new ShopPackageMock();

            shopCenter.AddPackage(package1);
            shopCenter.AddPackage(package2);

            Assert.That(shopCenter.Packages(), Contains.Item(package1));
            Assert.That(shopCenter.Packages(), Contains.Item(package2));
        }

        [Test]
        public void CanRemovePackages()
        {
            var package1 = new ShopPackageMock();
            var package2 = new ShopPackageMock();

            shopCenter.AddPackage(package1);
            shopCenter.AddPackage(package2);

            shopCenter.RemovePackage(package1); 

            Assert.That(shopCenter.Packages(), Has.No.Member(package1));
            Assert.That(shopCenter.Packages(), Has.Member(package2));
        }

        [Test]
        public void ShoudGivePackagesByType()
        {
            var packageA1 = new ShopPackageMockA();
            var packageA2 = new ShopPackageMockA();

            var packageB1 = new ShopPackageMockB();
            var packageB2 = new ShopPackageMockB();

            shopCenter.AddPackage(packageA1);
            shopCenter.AddPackage(packageA2);

            shopCenter.AddPackage(packageB1);
            shopCenter.AddPackage(packageB2);

            Assert.That(shopCenter.PackagesOfType<ShopPackageMockA>(), Contains.Item(packageA1));
            Assert.That(shopCenter.PackagesOfType<ShopPackageMockA>(), Contains.Item(packageA2));
            Assert.That(shopCenter.PackagesOfType<ShopPackageMockA>(), Has.Count.EqualTo(2));

            Assert.That(shopCenter.PackagesOfType<ShopPackageMockB>(), Contains.Item(packageB1));
            Assert.That(shopCenter.PackagesOfType<ShopPackageMockB>(), Contains.Item(packageB2));
            Assert.That(shopCenter.PackagesOfType<ShopPackageMockB>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void PurchasingShouldBeDelegatedToDesignatedPurchaseHandler()
        {
            var packageA = new ShopPackageMockA();
            var packageB = new ShopPackageMockB();

            var packageAPurchaseHandler = new PurchaseHandlerMock();
            var packageBPurchaseHandler = new PurchaseHandlerMock();

            shopCenter.AssignPurchaseHandler<ShopPackageMockA>(packageAPurchaseHandler);
            shopCenter.AssignPurchaseHandler<ShopPackageMockB>(packageBPurchaseHandler);

            shopCenter.Purchase(packageA, delegate { }, delegate { });

            Assert.That(packageAPurchaseHandler.givenShopPackage, Is.SameAs(packageA));
            Assert.That(packageBPurchaseHandler.givenShopPackage, Is.Null);


            packageAPurchaseHandler.Clear();
            packageBPurchaseHandler.Clear();

            shopCenter.Purchase(packageB, delegate { }, delegate { });

            Assert.That(packageAPurchaseHandler.givenShopPackage, Is.Null);
            Assert.That(packageBPurchaseHandler.givenShopPackage, Is.SameAs(packageB));
        }

        [Test]
        public void PurchasingShouldApplyThePackageWhenThePurchaseHandlerSucceedsPurchasing()
        {
            var package = new ShopPackageMockA();
            bool isPurchaseSuccessful = false;

            var packagePurchaseHandler = new PurchaseHandlerMock();
            packagePurchaseHandler.ShouldSucceed(true);


            shopCenter.AssignPurchaseHandler<ShopPackageMockA>(packagePurchaseHandler);

            shopCenter.Purchase(
                package, 
                onSuccess: (r) => isPurchaseSuccessful = true, 
                onFailure: delegate { });


            Assert.That(isPurchaseSuccessful, Is.True);
            Assert.That(package.IsApplied(), Is.True);
        }

        [Test]
        public void PurchasingShouldNotApplyThePackageWhenThePurchaseHandlerFailsPurchasing()
        {
            var package = new ShopPackageMockA();
            bool isPurchaseFailed = false;

            var packagePurchaseHandler = new PurchaseHandlerMock();
            packagePurchaseHandler.ShouldSucceed(false);


            shopCenter.AssignPurchaseHandler<ShopPackageMockA>(packagePurchaseHandler);

            shopCenter.Purchase(
                package,
                onSuccess: delegate { },
                onFailure: (r) => isPurchaseFailed = true);


            Assert.That(isPurchaseFailed, Is.True);
            Assert.That(package.IsApplied(), Is.False);
        }

        [Test]
        public void PurchasingShouldCallPurchaseSuccessCallbackWhenPurchasingIsSucceeded()
        {
            var package = new ShopPackageMockA();

            var packagePurchaseHandler = new PurchaseHandlerMock();
            packagePurchaseHandler.ShouldSucceed(true);
            shopCenter.AssignPurchaseHandler<ShopPackageMockA>(packagePurchaseHandler);

            ShopPackage purchasedPackage = null;
            shopCenter.SetPurchaseSuccessCallback((p, r) => purchasedPackage = p);
            
            shopCenter.Purchase(
                package,
                onSuccess: delegate { },
                onFailure: delegate { });


            Assert.That(purchasedPackage, Is.SameAs(package));
        }

        [Test]
        public void PurchasingShouldCallPurchaseFailureCallbackWhenPurchasingIsFailed()
        {
            var package = new ShopPackageMockA();

            var packagePurchaseHandler = new PurchaseHandlerMock();
            packagePurchaseHandler.ShouldSucceed(false);
            shopCenter.AssignPurchaseHandler<ShopPackageMockA>(packagePurchaseHandler);

            ShopPackage purchasedPackage = null;
            shopCenter.SetPurchaseFailureCallback((p, r) => purchasedPackage = p);

            shopCenter.Purchase(
                package,
                onSuccess: delegate { },
                onFailure: delegate { });


            Assert.That(purchasedPackage, Is.SameAs(package));
        }
    }
}