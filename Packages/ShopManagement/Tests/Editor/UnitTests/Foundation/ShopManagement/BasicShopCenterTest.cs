using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.ShopManagement.Core
{


    public class ShopPackageMock : ShopPackage
    {

    }

    public class ShopPackageMockA : ShopPackageMock
    {

    }

    public class ShopPackageMockB : ShopPackageMock
    {

    }

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
    }
}