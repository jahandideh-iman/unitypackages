
using Arman.InventorySystem;

namespace Arman.Mocks.Game.InventorySystem
{

    public class MockInventoryItemConstraint : IInventoryItemConstraint
    {
        public int givenValue;

        public int ApplyTo(int value)
        {
            givenValue = value;
            return value;
        }
    }
}