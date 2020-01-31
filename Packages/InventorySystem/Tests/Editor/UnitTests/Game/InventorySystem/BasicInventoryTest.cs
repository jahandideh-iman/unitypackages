
using Arman.Game.InventorySystem.Core;
using Arman.Mocks.Game.InventorySystem;
using NUnit.Framework;


namespace Arman.Tests.Game.InventorySystem.Core
{

    public class BasicInventoryTest
    {
        public class TestItemType : InventoryItem
        { }


        BasicInventory<TestItemType> inventory;
        TestItemType itemA;
        TestItemType itemB;

        [SetUp]
        public void Setup()
        {
            inventory = new BasicInventory<TestItemType>();
            itemA = new TestItemType();
            itemB = new TestItemType();
        }

        [Test]
        public void GettingTheValueOfASetItemShouldReturnGivenValue()
        {
            inventory.SetNumberOf(itemA, 3);
            inventory.SetNumberOf(itemB, 7);

            Assert.That(inventory.NumberOf(itemA), Is.EqualTo(3));
            Assert.That(inventory.NumberOf(itemB), Is.EqualTo(7));
        }

        [Test]
        public void IncreasingTheValueOfASetItemShouldIncreaseTheValueOfTheItem()
        {
            inventory.SetNumberOf(itemA, 3);

            inventory.Increase(itemA, 5);

            Assert.That(inventory.NumberOf(itemA), Is.EqualTo(3 + 5));
        }

        [Test]
        public void DecreasingTheValueOfASetItemShouldDecreaseTheValueOfTheItem()
        {
            inventory.SetNumberOf(itemA, 5);

            inventory.Decrease(itemA, 3);

            Assert.That(inventory.NumberOf(itemA), Is.EqualTo(5 - 3));
        }

        [Test]
        public void CanCheckForHavingASpecificValue()
        {
            inventory.SetNumberOf(itemA, 5);

            Assert.That(inventory.Has(itemA, 5), Is.True);
            Assert.That(inventory.Has(itemA, 3), Is.True);
            Assert.That(inventory.Has(itemA, 7), Is.False);
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldUseTheDefinedConstaintOnThatItem()
        {
            var mockConstraint = new MockInventoryItemConstraint();

            inventory.SetConstraint(itemA, mockConstraint);

            inventory.SetNumberOf(itemA, 5);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5));

            inventory.Increase(itemA, 3);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5 +3));

            inventory.Decrease(itemA, 1);
            Assert.That(mockConstraint.givenValue, Is.EqualTo(5 + 3 - 1));
        }
    }
}