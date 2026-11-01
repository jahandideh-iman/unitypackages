using NUnit.Framework;

namespace Arman.PackageBasics.Tests
{
    public class ContainerTest
    {
        interface IItem { }

        class ItemA : IItem { }

        class ItemB : IItem { }

        class ItemAChild : ItemA { }

        Container<IItem> _container = null!;

        [SetUp]
        public void Setup()
        {
            _container = new Container<IItem>();
        }

        [Test]
        public void ANewContainerShouldBeEmpty()
        {
            Assert.That(_container.Items(), Is.Empty);
        }

        [Test]
        public void ContainsTheAddedItems()
        {
            var itemA = new ItemA();
            var itemB = new ItemB();

            _container.Add(itemA);
            _container.Add(itemB);

            Assert.That(_container.Contains(itemA), Is.True);
            Assert.That(_container.Contains(itemB), Is.True);
            Assert.That(_container.Contains(new ItemA()), Is.False);
        }

        [Test]
        public void ItemsShouldReturnTheAddedItemsInOrder()
        {
            var itemA = new ItemA();
            var itemB = new ItemB();

            _container.Add(itemA);
            _container.Add(itemB);

            Assert.That(_container.Items(), Is.EqualTo(new IItem[] { itemA, itemB }));
        }

        [Test]
        public void FindingShouldReturnTheFirstItemOfTheType()
        {
            var first = new ItemA();
            var second = new ItemA();

            _container.Add(new ItemB());
            _container.Add(first);
            _container.Add(second);

            Assert.That(_container.Find<ItemA>(), Is.SameAs(first));
        }

        [Test]
        public void FindingShouldMatchItemsOfADerivedType()
        {
            var child = new ItemAChild();

            _container.Add(child);

            Assert.That(_container.Find<ItemA>(), Is.SameAs(child));
        }

        [Test]
        public void FindingAMissingTypeShouldReturnNull()
        {
            _container.Add(new ItemA());

            Assert.That(_container.Find<ItemB>(), Is.Null);
        }

        [Test]
        public void FindingAllShouldReturnEveryItemOfTheTypeInOrder()
        {
            var first = new ItemA();
            var child = new ItemAChild();

            _container.Add(first);
            _container.Add(new ItemB());
            _container.Add(child);

            Assert.That(_container.FindAll<ItemA>(), Is.EqualTo(new ItemA[] { first, child }));
        }

        [Test]
        public void FindingAllOfAMissingTypeShouldReturnAnEmptyCollection()
        {
            _container.Add(new ItemA());

            Assert.That(_container.FindAll<ItemB>(), Is.Empty);
        }

        [Test]
        public void AddingTheSameItemTwiceShouldKeepBothEntries()
        {
            var item = new ItemA();

            _container.Add(item);
            _container.Add(item);

            Assert.That(_container.Items(), Is.EqualTo(new IItem[] { item, item }));
        }
    }
}
