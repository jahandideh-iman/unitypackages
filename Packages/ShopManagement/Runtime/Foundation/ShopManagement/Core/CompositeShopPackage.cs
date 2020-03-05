

using System;
using System.Collections.Generic;

namespace Arman.Foundation.ShopManagement.Core
{
    public class CompositeShopPackage : ShopPackage
    {
        List<ShopPackage> packages = new List<ShopPackage>();

        public void Apply()
        {
            foreach (var package in packages)
                package.Apply();
        }

        public void Add(ShopPackage package)
        {
            packages.Add(package);
        }

        public ICollection<ShopPackage> Packages()
        {
            return packages;
        }
    }
}