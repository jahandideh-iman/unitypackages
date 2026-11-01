using NUnit.Framework;

namespace Arman.ComponentSystem.Tests
{
    interface ISpecialComponent : IComponent { }

    class SpecialComponentA : ISpecialComponent { }

    class SpecialComponentB : ISpecialComponent { }

    public class SpecializedEntityTest
    {
        SpecializedEntity<ISpecialComponent> _entity = null!;

        [SetUp]
        public void Setup()
        {
            _entity = new SpecializedEntity<ISpecialComponent>();
        }

        [Test]
        public void HasTheAddedComponents()
        {
            var componentA = new SpecialComponentA();
            var componentB = new SpecialComponentB();

            _entity.AddComponent(componentA);
            _entity.AddComponent(componentB);

            Assert.That(_entity.GetComponent<SpecialComponentA>(), Is.SameAs(componentA));
            Assert.That(_entity.GetComponent<SpecialComponentB>(), Is.SameAs(componentB));
        }

        [Test]
        public void GettingAMissingComponentShouldReturnNull()
        {
            _entity.AddComponent(new SpecialComponentA());

            Assert.That(_entity.GetComponent<SpecialComponentB>(), Is.Null);
        }

        [Test]
        public void AllComponentsShouldReturnTheAddedComponentsInOrder()
        {
            var componentA = new SpecialComponentA();
            var componentB = new SpecialComponentB();

            _entity.AddComponent(componentA);
            _entity.AddComponent(componentB);

            Assert.That(
                _entity.AllComponents(),
                Is.EqualTo(new ISpecialComponent[] { componentA, componentB })
            );
        }
    }
}
