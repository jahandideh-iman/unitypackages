using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public delegate void OnItemNumberChanged<T>(T item, int value);

    public interface Inventory<T> where T : InventoryItem
    {
        void SetNumberOf(T item, int number);
        void Increase(T item, int number);
        void Decrease(T item, int number);

        int NumberOf(T item);
        bool Has(T item, int number);

        void SetConstraint(T item, InventoryItemConstraint constraint);

        IEnumerable<T> Items();

        void SetOnValueChangeCallback(OnItemNumberChanged<T> callback);
    }
}