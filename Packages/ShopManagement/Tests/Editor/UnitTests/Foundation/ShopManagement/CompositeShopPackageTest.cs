using Arman.ShopManagement;
using Arman.Mocks.Foundation.ShopManagement.Core;
using NUnit.Framework;

namespace Arman.ShopManagement.Tests
{
    public class CompositeShopPackageTest
    {
        [Test]
        public void ApplyingShoudDelegateApplyingToAllComponents()
        {
            var composite = new CompositeShopPackage();

            var package1 = new ShopPackageMock();
            var package2 = new ShopPackageMock();

            composite.Add(package1);
            composite.Add(package2);

            composite.Apply();

            Assert.That(package1.IsApplied(), Is.True);
            Assert.That(package2.IsApplied(), Is.True);
        }
    }
}