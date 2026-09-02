namespace Arman.InventorySystem
{
    public class MinMaxInventoryItemConstraint : IInventoryItemConstraint
    {
        int min;
        int max;

        public MinMaxInventoryItemConstraint(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int ApplyTo(int value)
        {
            if (value > max)
                return max;
            else if (value < min)
                return min;
            else 
                return value;
        }
    }

}