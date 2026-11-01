using Arman.Mocks.Foundation.ShopManagement.Core;
using Arman.ShopManagement;
using NUnit.Framework;

namespace Arman.ShopManagement.Tests
{
    public class CompositeShopPackageTest
    {
        [Test]
        public void ApplyingShouldDelegateApplyingToAllComponents()
        {
            var composite = new CompositeShopPackage();

            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            composite.Add(package1);
            composite.Add(package2);

            composite.Apply();

            Assert.That(package1.IsApplied(), Is.True);
            Assert.That(package2.IsApplied(), Is.True);
        }

        [Test]
        public void ShouldHaveTheAddedPackages()
        {
            var composite = new CompositeShopPackage();

            var package1 = new FakeShopPackage();
            var package2 = new FakeShopPackage();

            composite.Add(package1);
            composite.Add(package2);

            Assert.That(
                composite.Packages(),
                Is.EqualTo(new IShopPackage[] { package1, package2 })
            );
        }

        [Test]
        public void ApplyingAnEmptyCompositeShouldDoNothing()
        {
            var composite = new CompositeShopPackage();

            Assert.That(() => composite.Apply(), Throws.Nothing);
        }

        [Test]
        public void ApplyingShouldReachPackagesInsideNestedComposites()
        {
            var outer = new CompositeShopPackage();
            var inner = new CompositeShopPackage();
            var package = new FakeShopPackage();

            inner.Add(package);
            outer.Add(inner);

            outer.Apply();

            Assert.That(package.IsApplied(), Is.True);
        }

        [Test]
        public void ACompositeShouldBePurchasableThroughAShopCenter()
        {
            var shopCenter = new ShopCenter();
            var composite = new CompositeShopPackage();
            var package = new FakeShopPackage();
            composite.Add(package);

            shopCenter.AssignPurchaseHandler<CompositeShopPackage>(
                FakePurchaseHandler.Succeeding(new FakePurchaseSuccessResult())
            );

            shopCenter.Purchase(composite, delegate { }, delegate { });

            Assert.That(package.IsApplied(), Is.True);
        }
    }
}
