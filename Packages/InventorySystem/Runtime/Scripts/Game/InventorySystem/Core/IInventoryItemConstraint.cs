namespace Arman.Game.InventorySystem.Core
{
    public interface IInventoryItemConstraint
    {
        int ApplyTo(int value);
    }
}