

using System;
using System.Collections.Generic;

namespace Arman.ShopManagement
{
    public class CompositeShopPackage : IShopPackage
    {
        List<IShopPackage> packages = new List<IShopPackage>();

        public void Apply()
        {
            foreach (var package in packages)
                package.Apply();
        }

        public void Add(IShopPackage package)
        {
            packages.Add(package);
        }

        public ICollection<IShopPackage> Packages()
        {
            return packages;
        }
    }
}