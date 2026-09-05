using System;
using Arman.ShopManagement;
using Arman.Mocks.Foundation.ShopManagement.Core;
using Moq;
using NUnit.Framework;

namespace Arman.ShopManagement.Tests
{
    public class FakeShopPackageA : FakeShopPackage {}

    public class FakeShopPackageB : FakeShopPackage {}


    public class ShopCenterTest
    {
        IShopCenter shopCenter;

        [SetUp]
        public void Setup()
        {
            shopCenter = new ShopCenter();
        }

        // A handler that answers every purchase the same way. Passing null as the
        // result matches what the shop center does with it: nothing.
        static Mock<IPurchaseHandler> PurchaseHandler(bool shouldSucceed)
        {
            var handler = new Mock<IPurchaseHandler>();

            handler
                .Setup(h => h.Purchase(
                    It.IsAny<IShopPackage>(),
                    It.IsAny<Action<IPurchaseSuccessResult>>(),
                    It.IsAny<Action<IPurchaseFailureResult>>()))
                .Callback<IShopPackage, Action<IPurchaseSuccessResult>, Action<IPurchaseFailureResult>>(
                    (package, onSuccess, onFailure) =>
                    {
                        if (shouldSucceed)
                            onSuccess(null);
                        else
                            onFailure(null);
                    });

            return handler;
        }

        [Test]
        public void ShoudHaveTheAddedPackages()
        {
            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            shopCenter.AddPackage(package1);
            shopCenter.AddPackage(package2);

            Assert.That(shopCenter.Packages(), Contains.Item(package1));
            Assert.That(shopCenter.Packages(), Contains.Item(package2));
        }

        [Test]
        public void CanRemovePackages()
        {
            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            shopCenter.AddPackage(package1);
            shopCenter.AddPackage(package2);

            shopCenter.RemovePackage(package1);

            Assert.That(shopCenter.Packages(), Has.No.Member(package1));
            Assert.That(shopCenter.Packages(), Has.Member(package2));
        }

        [Test]
        public void ShoudGivePackagesByType()
        {
            var packageA1 = new FakeShopPackageA();
            var packageA2 = new FakeShopPackageA();

            var packageB1 = new FakeShopPackageB();
            var packageB2 = new FakeShopPackageB();

            shopCenter.AddPackage(packageA1);
            shopCenter.AddPackage(packageA2);

            shopCenter.AddPackage(packageB1);
            shopCenter.AddPackage(packageB2);

            Assert.That(shopCenter.PackagesOfType<FakeShopPackageA>(), Contains.Item(packageA1));
            Assert.That(shopCenter.PackagesOfType<FakeShopPackageA>(), Contains.Item(packageA2));
            Assert.That(shopCenter.PackagesOfType<FakeShopPackageA>(), Has.Count.EqualTo(2));

            Assert.That(shopCenter.PackagesOfType<FakeShopPackageB>(), Contains.Item(packageB1));
            Assert.That(shopCenter.PackagesOfType<FakeShopPackageB>(), Contains.Item(packageB2));
            Assert.That(shopCenter.PackagesOfType<FakeShopPackageB>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void PurchasingShouldBeDelegatedToDesignatedPurchaseHandler()
        {
            var packageA = new FakeShopPackageA();
            var packageB = new FakeShopPackageB();

            var packageAPurchaseHandler = PurchaseHandler(shouldSucceed: false);
            var packageBPurchaseHandler = PurchaseHandler(shouldSucceed: false);

            shopCenter.AssignPurchaseHandler<FakeShopPackageA>(packageAPurchaseHandler.Object);
            shopCenter.AssignPurchaseHandler<FakeShopPackageB>(packageBPurchaseHandler.Object);

            shopCenter.Purchase(packageA, delegate { }, delegate { });

            packageAPurchaseHandler.Verify(
                h => h.Purchase(
                    packageA,
                    It.IsAny<Action<IPurchaseSuccessResult>>(),
                    It.IsAny<Action<IPurchaseFailureResult>>()),
                Times.Once);
            packageBPurchaseHandler.VerifyNoOtherCalls();

            shopCenter.Purchase(packageB, delegate { }, delegate { });

            packageBPurchaseHandler.Verify(
                h => h.Purchase(
                    packageB,
                    It.IsAny<Action<IPurchaseSuccessResult>>(),
                    It.IsAny<Action<IPurchaseFailureResult>>()),
                Times.Once);

            // Still exactly the one call it took above: packageB did not reach it.
            packageAPurchaseHandler.Verify(
                h => h.Purchase(
                    It.IsAny<IShopPackage>(),
                    It.IsAny<Action<IPurchaseSuccessResult>>(),
                    It.IsAny<Action<IPurchaseFailureResult>>()),
                Times.Once);
        }

        [Test]
        public void PurchasingShouldApplyThePackageWhenThePurchaseHandlerSucceedsPurchasing()
        {
            var package = new FakeShopPackageA();
            bool isPurchaseSuccessful = false;

            shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: true).Object);

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
            var package = new FakeShopPackageA();
            bool isPurchaseFailed = false;

            shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: false).Object);

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
            var package = new FakeShopPackageA();

            shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: true).Object);

            IShopPackage purchasedPackage = null;
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
            var package = new FakeShopPackageA();

            shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: false).Object);

            IShopPackage purchasedPackage = null;
            shopCenter.SetPurchaseFailureCallback((p, r) => purchasedPackage = p);

            shopCenter.Purchase(
                package,
                onSuccess: delegate { },
                onFailure: delegate { });


            Assert.That(purchasedPackage, Is.SameAs(package));
        }
    }
}
