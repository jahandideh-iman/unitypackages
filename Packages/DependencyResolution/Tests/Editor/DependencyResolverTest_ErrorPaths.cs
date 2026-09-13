using NUnit.Framework;
using System;

namespace Arman.DependencyResolution.Tests
{
    public class DependencyResolverTest_ErrorPaths
    {
        public class FactoryException : Exception { }

        public interface IShared { }
        public interface IUnrelated { }
        public class SharedImplementation1 : IShared { }
        public class SharedImplementation2 : IShared { }

        public class Missing { }
        public class RequiresMissing
        {
            public RequiresMissing(Missing missing) { }
        }

        public class CycleA
        {
            public CycleA(CycleB b) { }
        }

        public class CycleB
        {
            public CycleB(CycleA a) { }
        }

        public class SelfDependent
        {
            public SelfDependent(SelfDependent self) { }
        }

        public class ThrowingConstructor
        {
            public ThrowingConstructor() => throw new FactoryException();
        }

        public class NoPublicConstructor
        {
            private NoPublicConstructor() { }
        }

        ReflectionBasedDependecyResolver _dependecyResolver = null!;

        [SetUp]
        public void Setup()
        {
            _dependecyResolver = new ReflectionBasedDependecyResolver();
        }

        [Test]
        public void As_Throws_WhenTypeIsNotAssignableToTarget()
        {
            var entry = _dependecyResolver.RegisterType<SharedImplementation1>();

            Assert.That(() => entry.As<IUnrelated>(), Throws.Exception);
        }

        [Test]
        public void RegisterFactory_Throws_WhenReturnTypeIsNotAssignableToRegisteredType()
        {
            Func<SharedImplementation1> factory = () => new SharedImplementation1();

            Assert.That(() => _dependecyResolver.RegisterFactory<Missing>(factory), Throws.Exception);
        }

        [Test]
        public void RegisterFactory_Throws_WhenFactoryReturnsVoid()
        {
            Action factory = () => { };

            Assert.That(() => _dependecyResolver.RegisterFactory<Missing>(factory), Throws.Exception);
        }

        [Test]
        public void RegisterType_Throws_WhenTypeHasNoPublicConstructor()
        {
            Assert.That(() => _dependecyResolver.RegisterType<NoPublicConstructor>(), Throws.Exception);
        }

        [Test]
        public void Build_Throws_WhenADependencyIsNotRegistered()
        {
            _dependecyResolver.RegisterType<RequiresMissing>();

            Assert.That(() => _dependecyResolver.Build(), Throws.Exception);
        }

        [Test]
        public void Build_Throws_WhenMoreThanOneEntryProvidesTheSameType()
        {
            _dependecyResolver.RegisterType<SharedImplementation1>().As<IShared>();
            _dependecyResolver.RegisterType<SharedImplementation2>().As<IShared>();

            Assert.That(() => _dependecyResolver.Build(), Throws.Exception);
        }

        [Test]
        public void Build_Throws_WhenRegistrationsDependOnEachOther()
        {
            _dependecyResolver.RegisterType<CycleA>();
            _dependecyResolver.RegisterType<CycleB>();

            Assert.That(() => _dependecyResolver.Build(), Throws.Exception);
        }

        [Test]
        public void Build_Throws_WhenARegistrationDependsOnItself()
        {
            _dependecyResolver.RegisterType<SelfDependent>();

            Assert.That(() => _dependecyResolver.Build(), Throws.Exception);
        }

        [Test]
        public void Build_ThrowsTheFactoryException_WhenAFactoryThrows()
        {
            Func<Missing> factory = ThrowingFactory;
            _dependecyResolver.RegisterFactory<Missing>(factory);

            Assert.That(() => _dependecyResolver.Build(), Throws.TypeOf<FactoryException>());
        }

        [Test]
        public void Build_ThrowsTheConstructorException_WhenAConstructorThrows()
        {
            _dependecyResolver.RegisterType<ThrowingConstructor>();

            Assert.That(() => _dependecyResolver.Build(), Throws.TypeOf<FactoryException>());
        }

        [Test]
        public void Get_Throws_WhenTypeIsNotRegistered()
        {
            var result = _dependecyResolver.Build();

            Assert.That(() => result.Get<Missing>(), Throws.Exception);
        }

        [Test]
        public void TryGet_ReturnsFalse_WhenTypeIsNotRegistered()
        {
            var result = _dependecyResolver.Build();

            Assert.That(result.TryGet<Missing>(out _), Is.False);
        }

        private static Missing ThrowingFactory() => throw new FactoryException();
    }
}
