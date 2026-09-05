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
    }
}