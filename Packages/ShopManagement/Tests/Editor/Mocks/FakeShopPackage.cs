using Arman.ShopManagement;

namespace Arman.Mocks.Foundation.ShopManagement.Core
{
    // Stays a hand-written fake: ShopCenter.PackagesOfType<T>() and
    // AssignPurchaseHandler<T>() dispatch on the concrete type argument, which a
    // generated proxy type cannot express.
    public class FakeShopPackage : IShopPackage
    {
        bool _isApplied = false;

        public bool IsApplied()
        {
            return _isApplied;
        }

        public void Apply()
        {
            _isApplied = true;
        }
    }
}
