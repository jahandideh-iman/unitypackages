using Arman.InventorySystem;
using Moq;
using NUnit.Framework;

namespace Arman.InventorySystem.Tests
{
    public class InventoryTest
    {
        public class TestItemType : IInventoryItem { }

        Inventory<TestItemType> _inventory;
        TestItemType _itemA;
        TestItemType _itemB;

        [SetUp]
        public void Setup()
        {
            _inventory = new Inventory<TestItemType>();
            _itemA = new TestItemType();
            _itemB = new TestItemType();
        }

        [Test]
        public void GettingTheValueOfASetItemShouldReturnGivenValue()
        {
            _inventory.SetNumberOf(_itemA, 3);
            _inventory.SetNumberOf(_itemB, 7);

            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(3));
            Assert.That(_inventory.NumberOf(_itemB), Is.EqualTo(7));
        }

        [Test]
        public void IncreasingTheValueOfASetItemShouldIncreaseTheValueOfTheItem()
        {
            _inventory.SetNumberOf(_itemA, 3);

            _inventory.Increase(_itemA, 5);

            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(3 + 5));
        }

        [Test]
        public void DecreasingTheValueOfASetItemShouldDecreaseTheValueOfTheItem()
        {
            _inventory.SetNumberOf(_itemA, 5);

            _inventory.Decrease(_itemA, 3);

            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(5 - 3));
        }

        [Test]
        public void CanCheckForHavingASpecificValue()
        {
            _inventory.SetNumberOf(_itemA, 5);

            Assert.That(_inventory.Has(_itemA, 5), Is.True);
            Assert.That(_inventory.Has(_itemA, 3), Is.True);
            Assert.That(_inventory.Has(_itemA, 7), Is.False);
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldUseTheDefinedConstaintOnThatItem()
        {
            var constraint = new Mock<IInventoryItemConstraint>();
            constraint.Setup(c => c.ApplyTo(It.IsAny<int>())).Returns<int>(value => value);

            _inventory.SetConstraint(_itemA, constraint.Object);

            _inventory.SetNumberOf(_itemA, 5);
            constraint.Verify(c => c.ApplyTo(5), Times.Once);

            _inventory.Increase(_itemA, 3);
            constraint.Verify(c => c.ApplyTo(5 + 3), Times.Once);

            _inventory.Decrease(_itemA, 1);
            constraint.Verify(c => c.ApplyTo(5 + 3 - 1), Times.Once);
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldCallTheGivenGlobalCallbackWithCorrectValues()
        {
            TestItemType item = null;
            int value = 0;

            _inventory.SetGlobalOnValueChangeCallback(
                (i, nv) =>
                {
                    item = i;
                    value = nv;
                }
            );

            _inventory.SetNumberOf(_itemA, 5);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5));

            _inventory.SetNumberOf(_itemB, 3);
            Assert.That(item, Is.SameAs(_itemB));
            Assert.That(value, Is.EqualTo(3));

            _inventory.Increase(_itemA, 2);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5 + 2));

            _inventory.Decrease(_itemA, 3);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5 + 2 - 3));
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldCallTheGivenSpecificCallbackWithCorrectValues()
        {
            TestItemType item = null;
            int value = 0;

            _inventory.SetSpecificOnValueChangeCallback(
                _itemA,
                (i, nv) =>
                {
                    item = i;
                    value = nv;
                }
            );

            _inventory.SetNumberOf(_itemA, 5);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5));

            // Should not be call on ItemB.
            _inventory.SetNumberOf(_itemB, 3);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5));

            _inventory.Increase(_itemA, 2);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5 + 2));

            _inventory.Decrease(_itemA, 3);
            Assert.That(item, Is.SameAs(_itemA));
            Assert.That(value, Is.EqualTo(5 + 2 - 3));
        }

        [Test]
        public void CanChangeValuesWhileIteratingTheItems()
        {
            _inventory.SetNumberOf(_itemA, 1);
            _inventory.SetNumberOf(_itemB, 1);

            foreach (var item in _inventory.Items())
            {
                _inventory.Increase(item, 1);
                Assert.That(_inventory.NumberOf(item), Is.EqualTo(2));
            }
        }

        [Test]
        public void SettingTheValueOfAnItemAgainShouldReplaceThePreviousValue()
        {
            _inventory.SetNumberOf(_itemA, 3);

            _inventory.SetNumberOf(_itemA, 8);

            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(8));
        }

        [Test]
        public void ItemsShouldReturnEveryItemThatHasAValue()
        {
            _inventory.SetNumberOf(_itemA, 1);
            _inventory.SetNumberOf(_itemB, 0);

            Assert.That(_inventory.Items(), Is.EquivalentTo(new[] { _itemA, _itemB }));
        }

        [Test]
        public void ItemsOfANewInventoryShouldBeEmpty()
        {
            Assert.That(_inventory.Items(), Is.Empty);
        }

        [Test]
        public void ChangingTheValueOfAnItemShouldStoreTheConstrainedValue()
        {
            _inventory.SetConstraint(_itemA, new MinMaxInventoryItemConstraint(0, 10));

            _inventory.SetNumberOf(_itemA, 15);
            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(10));

            _inventory.Decrease(_itemA, 25);
            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(0));

            _inventory.Increase(_itemA, 4);
            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(4));
        }

        [Test]
        public void AConstraintShouldOnlyApplyToTheItemItWasSetFor()
        {
            _inventory.SetConstraint(_itemA, new MinMaxInventoryItemConstraint(0, 10));

            _inventory.SetNumberOf(_itemB, 15);

            Assert.That(_inventory.NumberOf(_itemB), Is.EqualTo(15));
        }

        [Test]
        public void SettingAConstraintShouldNotChangeTheCurrentValue()
        {
            _inventory.SetNumberOf(_itemA, 15);

            _inventory.SetConstraint(_itemA, new MinMaxInventoryItemConstraint(0, 10));

            Assert.That(_inventory.NumberOf(_itemA), Is.EqualTo(15));
        }

        [Test]
        public void CallbacksShouldReceiveTheConstrainedValue()
        {
            int globalValue = 0;
            int specificValue = 0;

            _inventory.SetConstraint(_itemA, new MinMaxInventoryItemConstraint(0, 10));
            _inventory.SetGlobalOnValueChangeCallback((i, nv) => globalValue = nv);
            _inventory.SetSpecificOnValueChangeCallback(_itemA, (i, nv) => specificValue = nv);

            _inventory.SetNumberOf(_itemA, 15);

            Assert.That(globalValue, Is.EqualTo(10));
            Assert.That(specificValue, Is.EqualTo(10));
        }

        [Test]
        public void SettingAGlobalCallbackShouldReplaceThePreviousOne()
        {
            int firstCalls = 0;
            int secondCalls = 0;

            _inventory.SetGlobalOnValueChangeCallback((i, nv) => firstCalls++);
            _inventory.SetGlobalOnValueChangeCallback((i, nv) => secondCalls++);

            _inventory.SetNumberOf(_itemA, 1);

            Assert.That(firstCalls, Is.EqualTo(0));
            Assert.That(secondCalls, Is.EqualTo(1));
        }

        [Test]
        public void SettingASpecificCallbackShouldReplaceThePreviousOneForThatItem()
        {
            int firstCalls = 0;
            int secondCalls = 0;

            _inventory.SetSpecificOnValueChangeCallback(_itemA, (i, nv) => firstCalls++);
            _inventory.SetSpecificOnValueChangeCallback(_itemA, (i, nv) => secondCalls++);

            _inventory.SetNumberOf(_itemA, 1);

            Assert.That(firstCalls, Is.EqualTo(0));
            Assert.That(secondCalls, Is.EqualTo(1));
        }

        [Test]
        public void HavingShouldBeFalseWhenTheValueIsBelowTheAskedNumber()
        {
            _inventory.SetNumberOf(_itemA, 0);

            Assert.That(_inventory.Has(_itemA, 1), Is.False);
            Assert.That(_inventory.Has(_itemA, 0), Is.True);
        }
    }
}
