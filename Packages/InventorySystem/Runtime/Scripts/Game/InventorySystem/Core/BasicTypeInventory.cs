using System;
using System.Collections;
using System.Collections.Generic;

namespace Arman.Game.InventorySystem.Core
{
    public class BasicTypeInventory<T> : TypeInventory<T> where T : InventoryItemType
    {
        struct InventoryTypeWrapper : InventoryItem
        {
            Type type;

            public InventoryTypeWrapper(Type type)
            {
                this.type = type;
            }

            public override bool Equals(object obj)
            {
                return obj is InventoryTypeWrapper wrapper &&
                       EqualityComparer<Type>.Default.Equals(type, wrapper.type);
            }

            public override int GetHashCode()
            {
                return 34944597 + EqualityComparer<Type>.Default.GetHashCode(type);
            }
        }

        BasicInventory<InventoryTypeWrapper> internalInventory = new BasicInventory<InventoryTypeWrapper>();

        public void SetNumberOf<S>(int number) where S : T
        {
            internalInventory.SetNumberOf(WrapperFor<S>(), number);
        }

        public void SetNumberOf(Type type, int number)
        {
            internalInventory.SetNumberOf(WrapperFor(type), number);
        }


        public void Increase<S>(int number) where S : T
        {
            internalInventory.Increase(WrapperFor<S>(), number);
        }

        public void Decrease<S>(int number) where S : T
        {
            internalInventory.Decrease(WrapperFor<S>(), number);
        }

        public int NumberOf<S>() where S : T
        {
            return internalInventory.NumberOf(WrapperFor<S>());
        }

        public int NumberOf(Type type)
        {
            return internalInventory.NumberOf(WrapperFor(type));
        }


        public bool Has<S>(int number) where S : T
        {
            return internalInventory.Has(WrapperFor<S>(), number);
        }

        public void SetConstraint<S>(InventoryItemConstraint constraint) where S : T
        {
            internalInventory.SetConstraint(WrapperFor<S>(), constraint);
        }

        InventoryTypeWrapper WrapperFor<S>()
        {
            return new InventoryTypeWrapper(typeof(S));
        }

        private InventoryTypeWrapper WrapperFor(Type type)
        {
            return new InventoryTypeWrapper(type);
        }

        IEnumerable<Type> TypeInventory<T>.ItemTypes()
        {
            throw new NotImplementedException();
        }
    }
}