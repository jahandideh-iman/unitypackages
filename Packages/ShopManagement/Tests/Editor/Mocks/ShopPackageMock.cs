using Arman.Foundation.ShopManagement.Core;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    public class ShopPackageMock : ShopPackage
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