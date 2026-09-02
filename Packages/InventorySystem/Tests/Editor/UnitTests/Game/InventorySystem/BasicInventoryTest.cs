
using Arman.InventorySystem;
using Arman.Mocks.Game.InventorySystem;
using NUnit.Framework;


namespace Arman.InventorySystem.Tests
{

    public class BasicInventoryTest
    {
        public class TestItemType : IInventoryItem
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

        [Test]
        public void ChangingTheValueOfAnItemShouldCallTheGivenGlobalCallbackWithCorrectValues()
        {
            TestItemType item = null;
            int value = 0;

            inventory.SetGlobalOnValueChangeCallback((i, nv) => { item = i; value = nv;});

            inventory.SetNumberOf(itemA, 5);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5));

            inventory.SetNumberOf(itemB, 3);
            Assert.That(item, Is.SameAs(itemB));
            Assert.That(value, Is.EqualTo(3));

            inventory.Increase(itemA, 2);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5 + 2));


            inventory.Decrease(itemA, 3);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5 + 2 - 3));
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldCallTheGivenSpecificCallbackWithCorrectValues()
        {
            TestItemType item = null;
            int value = 0;

            inventory.SetSpecificOnValueChangeCallback(itemA, (i, nv) => { item = i; value = nv; });

            inventory.SetNumberOf(itemA, 5);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5));

            // Should not be call on ItemB.
            inventory.SetNumberOf(itemB, 3);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5));

            inventory.Increase(itemA, 2);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5 + 2));


            inventory.Decrease(itemA, 3);
            Assert.That(item, Is.SameAs(itemA));
            Assert.That(value, Is.EqualTo(5 + 2 - 3));
        }

        [Test]
        public void CanChangeValuesWhileIteratingTheItems()
        {
            inventory.SetNumberOf(itemA, 1);
            inventory.SetNumberOf(itemB, 1);

            foreach(var item in inventory.Items())
            {
                inventory.Increase(item, 1);
                Assert.That(inventory.NumberOf(item), Is.EqualTo(2));
            }
        }
    }
}