using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public class BasicInventory<T> : IInventory<T> where T : IInventoryItem
    {
        class EmptyConstraint : IInventoryItemConstraint
        {
            public int ApplyTo(int value)
            {
                return value;
            }
        }

        Dictionary<T, int> itemNumbers = new Dictionary<T, int>();
        Dictionary<T, IInventoryItemConstraint> itemConstraints = new Dictionary<T, IInventoryItemConstraint>();

        IInventoryItemConstraint defaultConstraint = new EmptyConstraint();

        OnItemNumberChanged<T> globalOnItemNumberChangedCallback = delegate { };
        Dictionary<T, OnItemNumberChanged<T>> specificOnItemNumberChangedCallbacks = new Dictionary<T, OnItemNumberChanged<T>>();

        public void SetNumberOf(T item, int number)
        {
            var constraint = ConstraintFor(item);
            var value = constraint.ApplyTo(number);
            itemNumbers[item] = value;

            TryCallCallbacksFor(item, value);
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

        public void SetConstraint(T item, IInventoryItemConstraint constraint)
        {
            itemConstraints[item] = constraint;
        }

        //WARNING: It creates garbage.
        public IEnumerable<T> Items()
        {
            return new List<T>(itemNumbers.Keys);
        }

        public void SetGlobalOnValueChangeCallback(OnItemNumberChanged<T> callback)
        {
            globalOnItemNumberChangedCallback = callback;
        }

        public void SetSpecificOnValueChangeCallback(T target, OnItemNumberChanged<T> callback)
        {
            specificOnItemNumberChangedCallbacks[target] = callback;
        }

        private IInventoryItemConstraint ConstraintFor(T item)
        {
            IInventoryItemConstraint constraint;

            itemConstraints.TryGetValue(item, out constraint);

            if (constraint == null)
                return defaultConstraint;
            else
                return constraint;
        }

        private void TryCallCallbacksFor(T item, int value)
        {
            globalOnItemNumberChangedCallback.Invoke(item, value);

            if (specificOnItemNumberChangedCallbacks.ContainsKey(item))
                specificOnItemNumberChangedCallbacks[item].Invoke(item, value);
        }
    }
}