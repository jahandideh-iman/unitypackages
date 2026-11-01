using System;
using Arman.Mocks.Foundation.ShopManagement.Core;
using Arman.ShopManagement;
using Moq;
using NUnit.Framework;

namespace Arman.ShopManagement.Tests
{
    public class FakeShopPackageA : FakeShopPackage { }

    public class FakeShopPackageB : FakeShopPackage { }

    public class ShopCenterTest
    {
        IShopCenter _shopCenter;

        [SetUp]
        public void Setup()
        {
            _shopCenter = new ShopCenter();
        }

        // A handler that answers every purchase the same way. Passing null as the
        // result matches what the shop center does with it: nothing.
        static Mock<IPurchaseHandler> PurchaseHandler(bool shouldSucceed)
        {
            var handler = new Mock<IPurchaseHandler>();

            handler
                .Setup(h =>
                    h.Purchase(
                        It.IsAny<IShopPackage>(),
                        It.IsAny<Action<IPurchaseSuccessResult>>(),
                        It.IsAny<Action<IPurchaseFailureResult>>()
                    )
                )
                .Callback<
                    IShopPackage,
                    Action<IPurchaseSuccessResult>,
                    Action<IPurchaseFailureResult>
                >(
                    (package, onSuccess, onFailure) =>
                    {
                        if (shouldSucceed)
                            onSuccess(null);
                        else
                            onFailure(null);
                    }
                );

            return handler;
        }

        [Test]
        public void ShouldHaveTheAddedPackages()
        {
            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            _shopCenter.AddPackage(package1);
            _shopCenter.AddPackage(package2);

            Assert.That(_shopCenter.Packages(), Contains.Item(package1));
            Assert.That(_shopCenter.Packages(), Contains.Item(package2));
        }

        [Test]
        public void CanRemovePackages()
        {
            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            _shopCenter.AddPackage(package1);
            _shopCenter.AddPackage(package2);

            _shopCenter.RemovePackage(package1);

            Assert.That(_shopCenter.Packages(), Has.No.Member(package1));
            Assert.That(_shopCenter.Packages(), Has.Member(package2));
        }

        [Test]
        public void ShouldGivePackagesByType()
        {
            var packageA1 = new FakeShopPackageA();
            var packageA2 = new FakeShopPackageA();

            var packageB1 = new FakeShopPackageB();
            var packageB2 = new FakeShopPackageB();

            _shopCenter.AddPackage(packageA1);
            _shopCenter.AddPackage(packageA2);

            _shopCenter.AddPackage(packageB1);
            _shopCenter.AddPackage(packageB2);

            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageA>(), Contains.Item(packageA1));
            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageA>(), Contains.Item(packageA2));
            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageA>(), Has.Count.EqualTo(2));

            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageB>(), Contains.Item(packageB1));
            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageB>(), Contains.Item(packageB2));
            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageB>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void PurchasingShouldBeDelegatedToDesignatedPurchaseHandler()
        {
            var packageA = new FakeShopPackageA();
            var packageB = new FakeShopPackageB();

            var packageAPurchaseHandler = PurchaseHandler(shouldSucceed: false);
            var packageBPurchaseHandler = PurchaseHandler(shouldSucceed: false);

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(packageAPurchaseHandler.Object);
            _shopCenter.AssignPurchaseHandler<FakeShopPackageB>(packageBPurchaseHandler.Object);

            _shopCenter.Purchase(packageA, delegate { }, delegate { });

            packageAPurchaseHandler.Verify(
                h =>
                    h.Purchase(
                        packageA,
                        It.IsAny<Action<IPurchaseSuccessResult>>(),
                        It.IsAny<Action<IPurchaseFailureResult>>()
                    ),
                Times.Once
            );
            packageBPurchaseHandler.VerifyNoOtherCalls();

            _shopCenter.Purchase(packageB, delegate { }, delegate { });

            packageBPurchaseHandler.Verify(
                h =>
                    h.Purchase(
                        packageB,
                        It.IsAny<Action<IPurchaseSuccessResult>>(),
                        It.IsAny<Action<IPurchaseFailureResult>>()
                    ),
                Times.Once
            );

            // Still exactly the one call it took above: packageB did not reach it.
            packageAPurchaseHandler.Verify(
                h =>
                    h.Purchase(
                        It.IsAny<IShopPackage>(),
                        It.IsAny<Action<IPurchaseSuccessResult>>(),
                        It.IsAny<Action<IPurchaseFailureResult>>()
                    ),
                Times.Once
            );
        }

        [Test]
        public void PurchasingShouldApplyThePackageWhenThePurchaseHandlerSucceedsPurchasing()
        {
            var package = new FakeShopPackageA();
            bool isPurchaseSuccessful = false;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: true).Object
            );

            _shopCenter.Purchase(
                package,
                onSuccess: (r) => isPurchaseSuccessful = true,
                onFailure: delegate { }
            );

            Assert.That(isPurchaseSuccessful, Is.True);
            Assert.That(package.IsApplied(), Is.True);
        }

        [Test]
        public void PurchasingShouldNotApplyThePackageWhenThePurchaseHandlerFailsPurchasing()
        {
            var package = new FakeShopPackageA();
            bool isPurchaseFailed = false;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: false).Object
            );

            _shopCenter.Purchase(
                package,
                onSuccess: delegate { },
                onFailure: (r) => isPurchaseFailed = true
            );

            Assert.That(isPurchaseFailed, Is.True);
            Assert.That(package.IsApplied(), Is.False);
        }

        [Test]
        public void PurchasingShouldCallPurchaseSuccessCallbackWhenPurchasingIsSucceeded()
        {
            var package = new FakeShopPackageA();

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: true).Object
            );

            IShopPackage purchasedPackage = null;
            _shopCenter.SetPurchaseSuccessCallback((p, r) => purchasedPackage = p);

            _shopCenter.Purchase(package, onSuccess: delegate { }, onFailure: delegate { });

            Assert.That(purchasedPackage, Is.SameAs(package));
        }

        [Test]
        public void PurchasingShouldCallPurchaseFailureCallbackWhenPurchasingIsFailed()
        {
            var package = new FakeShopPackageA();

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                PurchaseHandler(shouldSucceed: false).Object
            );

            IShopPackage purchasedPackage = null;
            _shopCenter.SetPurchaseFailureCallback((p, r) => purchasedPackage = p);

            _shopCenter.Purchase(package, onSuccess: delegate { }, onFailure: delegate { });

            Assert.That(purchasedPackage, Is.SameAs(package));
        }

        [Test]
        public void ANewShopCenterShouldHaveNoPackages()
        {
            Assert.That(_shopCenter.Packages(), Is.Empty);
        }

        [Test]
        public void PackagesOfABaseTypeShouldIncludePackagesOfItsSubtypes()
        {
            var packageA = new FakeShopPackageA();
            var packageB = new FakeShopPackageB();

            _shopCenter.AddPackage(packageA);
            _shopCenter.AddPackage(packageB);

            Assert.That(
                _shopCenter.PackagesOfType<FakeShopPackage>(),
                Is.EquivalentTo(new FakeShopPackage[] { packageA, packageB })
            );
        }

        [Test]
        public void PackagesOfATypeThatWasNeverAddedShouldBeEmpty()
        {
            _shopCenter.AddPackage(new FakeShopPackageA());

            Assert.That(_shopCenter.PackagesOfType<FakeShopPackageB>(), Is.Empty);
        }

        [Test]
        public void APurchaseHandlerAssignedToABaseTypeShouldHandleItsSubtypes()
        {
            var package = new FakeShopPackageA();
            var handler = FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult());

            _shopCenter.AssignPurchaseHandler<FakeShopPackage>(handler);

            _shopCenter.Purchase(package, delegate { }, delegate { });

            Assert.That(handler.PurchasedPackages, Is.EqualTo(new IShopPackage[] { package }));
            Assert.That(package.IsApplied(), Is.True);
        }

        [Test]
        public void TheFirstAssignedMatchingPurchaseHandlerShouldHandleThePurchase()
        {
            var package = new FakeShopPackageA();
            var first = FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult());
            var second = FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult());

            _shopCenter.AssignPurchaseHandler<FakeShopPackage>(first);
            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(second);

            _shopCenter.Purchase(package, delegate { }, delegate { });

            Assert.That(first.PurchasedPackages, Is.EqualTo(new IShopPackage[] { package }));
            Assert.That(second.PurchasedPackages, Is.Empty);
        }

        [Test]
        public void PurchasingShouldGiveTheHandlersSuccessResultToBothCallbacks()
        {
            var package = new FakeShopPackageA();
            var result = new FakePurchaseSuccessResult();
            IPurchaseSuccessResult? localResult = null;
            IPurchaseSuccessResult? globalResult = null;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Succeeding(result)
            );
            _shopCenter.SetPurchaseSuccessCallback((p, r) => globalResult = r);

            _shopCenter.Purchase(package, onSuccess: r => localResult = r, onFailure: delegate { });

            Assert.That(localResult, Is.SameAs(result));
            Assert.That(globalResult, Is.SameAs(result));
        }

        [Test]
        public void PurchasingShouldGiveTheHandlersFailureResultToBothCallbacks()
        {
            var package = new FakeShopPackageA();
            var result = new FakePurchaseFailureResult();
            IPurchaseFailureResult? localResult = null;
            IPurchaseFailureResult? globalResult = null;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Failing(result)
            );
            _shopCenter.SetPurchaseFailureCallback((p, r) => globalResult = r);

            _shopCenter.Purchase(package, onSuccess: delegate { }, onFailure: r => localResult = r);

            Assert.That(localResult, Is.SameAs(result));
            Assert.That(globalResult, Is.SameAs(result));
        }

        [Test]
        public void AFailedPurchaseShouldNotCallTheGlobalSuccessCallback()
        {
            var package = new FakeShopPackageA();
            bool successCalled = false;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Failing(new FakePurchaseFailureResult())
            );
            _shopCenter.SetPurchaseSuccessCallback((p, r) => successCalled = true);

            _shopCenter.Purchase(package, delegate { }, delegate { });

            Assert.That(successCalled, Is.False);
        }

        [Test]
        public void ASucceededPurchaseShouldNotCallTheGlobalFailureCallback()
        {
            var package = new FakeShopPackageA();
            bool failureCalled = false;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult())
            );
            _shopCenter.SetPurchaseFailureCallback((p, r) => failureCalled = true);

            _shopCenter.Purchase(package, delegate { }, delegate { });

            Assert.That(failureCalled, Is.False);
        }

        [Test]
        public void ThePackageShouldBeAppliedBeforeThePurchaseCallbacksRun()
        {
            var package = new FakeShopPackageA();
            bool appliedWhenLocalCalled = false;
            bool appliedWhenGlobalCalled = false;

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult())
            );
            _shopCenter.SetPurchaseSuccessCallback(
                (p, r) => appliedWhenGlobalCalled = package.IsApplied()
            );

            _shopCenter.Purchase(
                package,
                onSuccess: r => appliedWhenLocalCalled = package.IsApplied(),
                onFailure: delegate { }
            );

            Assert.That(appliedWhenLocalCalled, Is.True);
            Assert.That(appliedWhenGlobalCalled, Is.True);
        }

        [Test]
        public void PurchasingShouldNotRequireThePackageToBeAddedToTheShopCenter()
        {
            var package = new FakeShopPackageA();

            _shopCenter.AssignPurchaseHandler<FakeShopPackageA>(
                FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult())
            );

            _shopCenter.Purchase(package, delegate { }, delegate { });

            Assert.That(package.IsApplied(), Is.True);
            Assert.That(_shopCenter.Packages(), Is.Empty);
        }
    }
}
