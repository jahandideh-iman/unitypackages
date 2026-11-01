using NUnit.Framework;

namespace Arman.ComponentSystem.Tests
{
    class ComponentA : IComponent { };

    class ComponentB : IComponent { };

    class ComponentC : IComponent { };

    class ComponentParent : IComponent { };

    class ComponentChild : ComponentParent { };

    public class EntityTest
    {
        [Test]
        public void HasTheAddedComponents()
        {
            var entity = new Entity();

            var componentA = new ComponentA();
            var componentB = new ComponentB();

            entity.AddComponent(componentA);
            entity.AddComponent(componentB);

            Assert.That(entity.GetComponent<ComponentA>(), Is.SameAs(componentA));
            Assert.That(entity.GetComponent<ComponentB>(), Is.SameAs(componentB));
        }

        [Test]
        public void ShouldFindComponentByParentType()
        {
            var entity = new Entity();

            var componentChild = new ComponentChild();

            entity.AddComponent(componentChild);

            Assert.That(entity.GetComponent<ComponentParent>(), Is.SameAs(componentChild));
        }

        [Test]
        public void GettingAMissingComponentShouldReturnNull()
        {
            var entity = new Entity();

            entity.AddComponent(new ComponentA());

            Assert.That(entity.GetComponent<ComponentB>(), Is.Null);
        }

        [Test]
        public void AllComponentsShouldReturnTheAddedComponentsInOrder()
        {
            var entity = new Entity();

            var componentA = new ComponentA();
            var componentB = new ComponentB();
            var componentC = new ComponentC();

            entity.AddComponent(componentA);
            entity.AddComponents(componentB, componentC);

            Assert.That(
                entity.AllComponents(),
                Is.EqualTo(new IComponent[] { componentA, componentB, componentC })
            );
        }

        [Test]
        public void AddingSeveralComponentsAtOnceShouldMakeEachOneFindable()
        {
            var entity = new Entity();

            var componentA = new ComponentA();
            var componentB = new ComponentB();

            entity.AddComponents(componentA, componentB);

            Assert.That(entity.GetComponent<ComponentA>(), Is.SameAs(componentA));
            Assert.That(entity.GetComponent<ComponentB>(), Is.SameAs(componentB));
        }

        [Test]
        public void GettingAComponentShouldReturnTheFirstMatchAndGettingFromTheEndTheLast()
        {
            var entity = new Entity();

            var first = new ComponentA();
            var last = new ComponentA();

            entity.AddComponents(first, new ComponentB(), last);

            Assert.That(entity.GetComponent<ComponentA>(), Is.SameAs(first));
            Assert.That(entity.GetComponentFromEnd<ComponentA>(), Is.SameAs(last));
        }

        [Test]
        public void GettingAMissingComponentFromTheEndShouldReturnNull()
        {
            var entity = new Entity();

            entity.AddComponent(new ComponentA());

            Assert.That(entity.GetComponentFromEnd<ComponentB>(), Is.Null);
        }

        [Test]
        public void GettingAComponentByIndexShouldReturnTheComponentAddedAtThatPosition()
        {
            var entity = new Entity();

            var componentA = new ComponentA();
            var componentB = new ComponentB();

            entity.AddComponents(componentA, componentB);

            Assert.That(entity.GetComponent<ComponentA>(0), Is.SameAs(componentA));
            Assert.That(entity.GetComponent<ComponentB>(1), Is.SameAs(componentB));
        }
    }
}
