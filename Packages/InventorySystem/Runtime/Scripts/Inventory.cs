using System.Collections.Generic;

namespace Arman.InventorySystem
{
    public class Inventory<T> : IInventory<T>
        where T : IInventoryItem
    {
        class EmptyConstraint : IInventoryItemConstraint
        {
            public int ApplyTo(int value)
            {
                return value;
            }
        }

        Dictionary<T, int> _itemNumbers = new Dictionary<T, int>();
        Dictionary<T, IInventoryItemConstraint> _itemConstraints =
            new Dictionary<T, IInventoryItemConstraint>();

        IInventoryItemConstraint _defaultConstraint = new EmptyConstraint();

        OnItemNumberChanged<T> _globalOnItemNumberChangedCallback = delegate { };
        Dictionary<T, OnItemNumberChanged<T>> _specificOnItemNumberChangedCallbacks =
            new Dictionary<T, OnItemNumberChanged<T>>();

        public void SetNumberOf(T item, int number)
        {
            var constraint = ConstraintFor(item);
            var value = constraint.ApplyTo(number);
            _itemNumbers[item] = value;

            TryCallCallbacksFor(item, value);
        }

        public void Increase(T item, int number)
        {
            SetNumberOf(item, _itemNumbers[item] + number);
        }

        public void Decrease(T item, int number)
        {
            SetNumberOf(item, _itemNumbers[item] - number);
        }

        public int NumberOf(T item)
        {
            return _itemNumbers[item];
        }

        public bool Has(T item, int number)
        {
            return _itemNumbers[item] >= number;
        }

        public void SetConstraint(T item, IInventoryItemConstraint constraint)
        {
            _itemConstraints[item] = constraint;
        }

        //WARNING: It creates garbage.
        public IEnumerable<T> Items()
        {
            return new List<T>(_itemNumbers.Keys);
        }

        public void SetGlobalOnValueChangeCallback(OnItemNumberChanged<T> callback)
        {
            _globalOnItemNumberChangedCallback = callback;
        }

        public void SetSpecificOnValueChangeCallback(T target, OnItemNumberChanged<T> callback)
        {
            _specificOnItemNumberChangedCallbacks[target] = callback;
        }

        private IInventoryItemConstraint ConstraintFor(T item)
        {
            IInventoryItemConstraint constraint;

            _itemConstraints.TryGetValue(item, out constraint);

            if (constraint == null)
                return _defaultConstraint;
            else
                return constraint;
        }

        private void TryCallCallbacksFor(T item, int value)
        {
            _globalOnItemNumberChangedCallback.Invoke(item, value);

            if (_specificOnItemNumberChangedCallbacks.ContainsKey(item))
                _specificOnItemNumberChangedCallbacks[item].Invoke(item, value);
        }
    }
}
