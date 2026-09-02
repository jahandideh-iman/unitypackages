namespace Arman.InventorySystem
{
    public interface IInventoryItemConstraint
    {
        int ApplyTo(int value);
    }
}