using NUnit.Framework;

namespace Arman.DevelopmentConsole.Tests
{
    public class ReflectionUtilitiesTest
    {
        public interface IFakeOptions { }

        public abstract class AbstractFakeOptions : IFakeOptions { }

        public class ConcreteFakeOptions : AbstractFakeOptions { }

        public class OtherConcreteFakeOptions : IFakeOptions { }

        [Test]
        public void FindingTypesWithoutAbstractsShouldReturnOnlyTheConcreteAssignableTypes()
        {
            var types = ReflectionUtilities.FindTypesOf(typeof(IFakeOptions), false);

            Assert.That(
                types,
                Is.EquivalentTo(
                    new[] { typeof(ConcreteFakeOptions), typeof(OtherConcreteFakeOptions) }
                )
            );
        }

        [Test]
        public void FindingTypesWithAbstractsShouldAlsoReturnTheInterfaceAndAbstractTypes()
        {
            var types = ReflectionUtilities.FindTypesOf(typeof(IFakeOptions), true);

            Assert.That(
                types,
                Is.EquivalentTo(
                    new[]
                    {
                        typeof(IFakeOptions),
                        typeof(AbstractFakeOptions),
                        typeof(ConcreteFakeOptions),
                        typeof(OtherConcreteFakeOptions),
                    }
                )
            );
        }

        [Test]
        public void GettingATypeByItsFullNameShouldFindItInAnyLoadedAssembly()
        {
            var type = ReflectionUtilities.GetType(typeof(ConcreteFakeOptions).FullName!);

            Assert.That(type, Is.EqualTo(typeof(ConcreteFakeOptions)));
        }

        [Test]
        public void GettingAnUnknownTypeShouldReturnNull()
        {
            Assert.That(
                ReflectionUtilities.GetType("Arman.DevelopmentConsole.Tests.Missing"),
                Is.Null
            );
        }

        [Test]
        public void AsShouldCastTheObjectToTheGivenType()
        {
            object options = new ConcreteFakeOptions();

            Assert.That(options.As<AbstractFakeOptions>(), Is.SameAs(options));
        }
    }
}
