using NUnit.Framework;

namespace Arman.DependencyResolution.Tests
{
    public class DependencyResolverTest_MultiTypeTargetRegistration
    {
        public interface IInterface1 { }

        public interface IInterface2 { }

        public class MultiInterfaceClass : IInterface1, IInterface2 { }

        public class ClassWithInterfaceArgument
        {
            public ClassWithInterfaceArgument(IInterface1 interface1, IInterface2 interface2) { }
        }

        ReflectionBasedDependencyResolver _dependencyResolver = null!;

        [SetUp]
        public void Setup()
        {
            _dependencyResolver = new ReflectionBasedDependencyResolver();
        }

        [Test]
        public void Build_ResolvesRegistrationAsOneInstance_WhenARegistrationTargetsMultipleTypes()
        {
            _dependencyResolver
                .RegisterType<MultiInterfaceClass>()
                .As<IInterface1>()
                .As<IInterface2>();

            var result = _dependencyResolver.Build();

            var instance = result.Get<MultiInterfaceClass>();

            Assert.That(result.Get<IInterface1>(), Is.SameAs(instance));
            Assert.That(result.Get<IInterface2>(), Is.SameAs(instance));
        }

        [Test]
        public void Build_ResolvesRegistrations_WhenARegistrationRequiresOtherTargetTypes()
        {
            _dependencyResolver
                .RegisterType<MultiInterfaceClass>()
                .As<IInterface1>()
                .As<IInterface2>();
            _dependencyResolver.RegisterType<ClassWithInterfaceArgument>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<ClassWithInterfaceArgument>(), Is.Not.Null);
        }
    }
}
