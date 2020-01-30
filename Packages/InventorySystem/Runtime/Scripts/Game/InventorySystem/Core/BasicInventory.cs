using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public class BasicInventory<T> : Inventory<T> where T : InventoryItem
    {
        class EmptyConstraint : InventoryItemConstraint
        {
            public int ApplyTo(int value)
            {
                return value;
            }
        }

        Dictionary<T, int> itemNumbers = new Dictionary<T, int>();
        Dictionary<T, InventoryItemConstraint> itemConstraints = new Dictionary<T, InventoryItemConstraint>();

        InventoryItemConstraint defaultConstraint = new EmptyConstraint();

        public void SetNumberOf(T item, int number)
        {
            var constraint = ConstraintFor(item);

            itemNumbers[item] = constraint.ApplyTo(number);
        }

        public void Increase(T item, int number)
        {
            SetNumberOf(item, itemNumbers[item] + number);
        }

        public void Decrease(T item, int number)
        {
            SetNumberOf(item, itemNumbers[item] - number);
        }

        public int NumberOf(T item)
        {
            return itemNumbers[item];
        }

        public bool Has(T item, int number)
        {
            return itemNumbers[item] >= number;
        }

        public void SetConstraint(T item, InventoryItemConstraint constraint)
        {
            itemConstraints[item] = constraint;
        }

        private InventoryItemConstraint ConstraintFor(T item)
        {
            InventoryItemConstraint constraint;

            itemConstraints.TryGetValue(item, out constraint);

            if (constraint == null)
                return defaultConstraint;
            else
                return constraint;
        }

        public IEnumerable<T> Items()
        {
            return itemNumbers.Keys;
        }
    }
}