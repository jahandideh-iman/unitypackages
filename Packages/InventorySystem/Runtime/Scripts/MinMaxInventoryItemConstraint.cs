namespace Arman.InventorySystem
{
    public class MinMaxInventoryItemConstraint : IInventoryItemConstraint
    {
        int _min;
        int _max;

        public MinMaxInventoryItemConstraint(int min, int max)
        {
            this._min = min;
            this._max = max;
        }

        public int ApplyTo(int value)
        {
            if (value > _max)
                return _max;
            else if (value < _min)
                return _min;
            else
                return value;
        }
    }
}
