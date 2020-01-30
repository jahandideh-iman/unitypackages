using System;
using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public interface TypeInventory<T>  where T : InventoryItemType
    {
        void SetNumberOf<S>(int number) where S : T;
        void SetNumberOf(Type type, int number);

        void Increase<S>(int number) where S : T;
        void Decrease<S>(int number) where S : T;

        int NumberOf<S>() where S : T;
        int NumberOf(Type type);

        bool Has<S>(int number) where S : T;

        void SetConstraint<S>(InventoryItemConstraint constraint) where S : T;

        IEnumerable<Type> ItemTypes();
    }
}