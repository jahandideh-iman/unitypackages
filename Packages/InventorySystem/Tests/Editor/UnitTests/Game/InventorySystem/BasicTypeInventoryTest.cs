

using Arman.Game.InventorySystem.Core;
using Arman.Mocks.Game.InventorySystem;
using NUnit.Framework;


namespace Arman.Tests.Game.InventorySystem.Core
{
    public class BasicTypeInventoryTest
    {
        public class BaseItemType : InventoryItemType
        { }

        public class ItemTypeA : BaseItemType
        { }

        public class ItemTypeB : BaseItemType
        { }


        BasicTypeInventory<BaseItemType> inventory;


        [SetUp]
        public void Setup()
        {
            inventory = new BasicTypeInventory<BaseItemType>();

        }

        // TODO: Sperate the generic interface test from type-based interface test.
        [Test]
        public void GettingTheValueOfASetItemShouldReturnGivenValue()
        {
            inventory.SetNumberOf<ItemTypeA>(3);
            inventory.SetNumberOf<ItemTypeB>(7);

            Assert.That(inventory.NumberOf(typeof(ItemTypeA)), Is.EqualTo(3));
            Assert.That(inventory.NumberOf(typeof(ItemTypeB)), Is.EqualTo(7));

            inventory.SetNumberOf(typeof(ItemTypeA), 2);
            inventory.SetNumberOf(typeof(ItemTypeB), 6);

            Assert.That(inventory.NumberOf<ItemTypeA>(), Is.EqualTo(2));
            Assert.That(inventory.NumberOf<ItemTypeB>(), Is.EqualTo(6));
        }

        [Test]
        public void IncreasingTheValueOfASetItemShouldIncreaseTheValueOfTheItem()
        {
            inventory.SetNumberOf<ItemTypeA>(3);

            inventory.Increase<ItemTypeA>( 5);

            Assert.That(inventory.NumberOf<ItemTypeA>(), Is.EqualTo(3 + 5));
        }

        [Test]
        public void DecreasingTheValueOfASetItemShouldDecreaseTheValueOfTheItem()
        {
            inventory.SetNumberOf<ItemTypeA>( 5);

            inventory.Decrease<ItemTypeA>( 3);

            Assert.That(inventory.NumberOf<ItemTypeA>(), Is.EqualTo(5 - 3));
        }

        [Test]
        public void CanCheckForHavingASpecificValue()
        {
            inventory.SetNumberOf<ItemTypeA>( 5);

            Assert.That(inventory.Has<ItemTypeA>( 5), Is.True);
            Assert.That(inventory.Has<ItemTypeA>( 3), Is.True);
            Assert.That(inventory.Has<ItemTypeA>( 7), Is.False);
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldUseTheDefinedConstaintOnThatItem()
        {
            var mockConstraint = new MockInventoryItemConstraint();

            inventory.SetConstraint<ItemTypeA>( mockConstraint);

            inventory.SetNumberOf<ItemTypeA>( 5);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5));

            inventory.Increase<ItemTypeA>( 3);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5 +3));

            inventory.Decrease<ItemTypeA>( 1);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5 + 3 - 1));
        }
    }
}