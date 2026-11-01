using System;
using System.Collections.Generic;

namespace Arman.ShopManagement
{
    public class CompositeShopPackage : IShopPackage
    {
        List<IShopPackage> _packages = new List<IShopPackage>();

        public void Apply()
        {
            foreach (var package in _packages)
                package.Apply();
        }

        public void Add(IShopPackage package)
        {
            _packages.Add(package);
        }

        public ICollection<IShopPackage> Packages()
        {
            return _packages;
        }
    }
}
