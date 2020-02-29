
using System.Collections.Generic;
using System.Linq;

public class BasicShopCenter : ShopCenter
{
    List<ShopPackage> packages = new List<ShopPackage>();

    public void AddPackage(ShopPackage package)
    {
        packages.Add(package);
    }

    public ICollection<ShopPackage> Packages()
    {
        return packages;
    }

    public ICollection<T> PackagesOfType<T>() where T : ShopPackage
    {
        return packages.Where(p => p is T).Cast<T>().ToList();
    }
}
