
using Arman.Utility.Core;
using NUnit.Framework;


namespace Arman.Tests.Utility.Core
{


    public class BaseType { };

    public class DerivedTypeA : BaseType { };
    public class DerivedTypeB : BaseType {};

    public class BasicContainerTest
    {
        [Test]
        public void HasTheAddedItems()
        {
            var container = new BasicContainer<object>();

            container.Add(1);
            container.Add(2);

            Assert.That(container.Contains(1));
            Assert.That(container.Contains(2));
            Assert.That(container.Items(), Has.Member(1));
            Assert.That(container.Items(), Has.Member(2));
        }

        [Test]
        public void CanFindAnItemByType()
        {
            var container = new BasicContainer<BaseType>();

            var a = new DerivedTypeA();
            var b = new DerivedTypeB();

            container.Add(a);
            container.Add(b);

            Assert.That(container.Find<DerivedTypeA>(), Is.SameAs(a));
            Assert.That(container.Find<DerivedTypeB>(), Is.SameAs(b));
        }

        [Test]
        public void CanFindMultipleItemsByType()
        {
            var container = new BasicContainer<BaseType>();

            var a1 = new DerivedTypeA();
            var a2 = new DerivedTypeA();

            container.Add(a1);
            container.Add(a2);

            var items = container.FindAll<DerivedTypeA>();

            Assert.That(items, Has.Member(a1));
            Assert.That(items, Has.Member(a2));
        }
    }
}
