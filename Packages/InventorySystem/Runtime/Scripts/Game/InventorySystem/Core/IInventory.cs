using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public delegate void OnItemNumberChanged<T>(T item, int value);

    public interface IInventory<T> where T : IInventoryItem
    {
        void SetNumberOf(T item, int number);
        void Increase(T item, int number);
        void Decrease(T item, int number);

        int NumberOf(T item);
        bool Has(T item, int number);

        void SetConstraint(T item, IInventoryItemConstraint constraint);

        IEnumerable<T> Items();

        void SetGlobalOnValueChangeCallback(OnItemNumberChanged<T> callback);
        void SetSpecificOnValueChangeCallback(T target, OnItemNumberChanged<T> callback);
    }
}