using Arman.Foundation.ComponentSystem.Core;
using NUnit.Framework;

namespace Arman.Tests.Foundation.ComponentSystem.Core
{
    class ComponentA : Component { };
    class ComponentB : Component { };
    class ComponentC : Component { };

    class ComponentParent : Component { };
    class ComponentChild : ComponentParent { };

    public class BasicEntityTest 
    {

        [Test]
        public void HasTheAddedComponents()
        {
            var entity = new BasicEntity();

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
            var entity = new BasicEntity();

            var componentChild = new ComponentChild();

            entity.AddComponent(componentChild);

            Assert.That(entity.GetComponent<ComponentParent>(), Is.SameAs(componentChild));
        }
    }
}