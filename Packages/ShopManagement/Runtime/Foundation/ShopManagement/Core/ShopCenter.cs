
using System.Collections.Generic;

public interface ShopCenter
{
    void AddPackage(ShopPackage package);
    ICollection<ShopPackage> Packages();
    ICollection<T> PackagesOfType<T>() where T : ShopPackage;
}
