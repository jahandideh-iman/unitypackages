using Arman.ShopManagement;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class ShopPackageMock : IShopPackage
    {
        bool isApplied = false;

        public bool IsApplied()
        {
            return isApplied;
        }

        public void Apply()
        {
            isApplied = true;
        }
    }
}