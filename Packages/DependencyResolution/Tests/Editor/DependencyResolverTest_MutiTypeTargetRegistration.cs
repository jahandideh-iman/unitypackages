using NUnit.Framework;

namespace Arman.DependencyResolution.Tests
{
    public class DependencyResolverTest_MutiTypeTargetRegistration
    {

        public interface IInterface1 { }
        public interface IInterface2 { }
        public class MultiIntefaceClass : IInterface1, IInterface2 { }
        public class ClassWithInterfaceArgument
        {
            public ClassWithInterfaceArgument(IInterface1 interface1, IInterface2 interface2) { }
        }

        ReflectionBasedDependecyResolver _dependecyResolver = null!;

        [SetUp]
        public void Setup()
        {
            _dependecyResolver = new ReflectionBasedDependecyResolver();
        }


        [Test]
        public void Build_ResolvesRegistrationAsOneInstance_WhenARegisterationTargetsMultipleTypes()
        {
            _dependecyResolver.RegisterType<MultiIntefaceClass>().As<IInterface1>().As<IInterface2>();

            var result = _dependecyResolver.Build();

            var instance = result.Get<MultiIntefaceClass>();

            Assert.That(result.Get<IInterface1>(), Is.SameAs(instance));
            Assert.That(result.Get<IInterface2>(), Is.SameAs(instance));
        }

        [Test]
        public void Build_ResolvesRegistrations_WhenARegisterationRequiresOtherTargetTypes()
        {
            _dependecyResolver.RegisterType<MultiIntefaceClass>().As<IInterface1>().As<IInterface2>();
            _dependecyResolver.RegisterType<ClassWithInterfaceArgument>();

            var result = _dependecyResolver.Build();

            Assert.That(result.Get<ClassWithInterfaceArgument>(), Is.Not.Null);
        }
    }
}