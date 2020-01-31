
using Arman.Game.InventorySystem.Core;

namespace Arman.Mocks.Game.InventorySystem
{

    public class MockInventoryItemConstraint : InventoryItemConstraint
    {
        public int givenValue;

        public int ApplyTo(int value)
        {
            givenValue = value;
            return value;
        }
    }
}