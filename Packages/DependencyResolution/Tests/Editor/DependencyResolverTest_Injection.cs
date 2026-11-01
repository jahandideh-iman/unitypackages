using System;
using NUnit.Framework;

namespace Arman.DependencyResolution.Tests
{
    public class DependencyResolverTest_Injection
    {
        public interface IService { }

        public class Service : IService { }

        public class Leaf { }

        public class Middle
        {
            public readonly Leaf Leaf;

            public Middle(Leaf leaf)
            {
                Leaf = leaf;
            }
        }

        public class Top
        {
            public readonly Leaf Leaf;
            public readonly Middle Middle;

            public Top(Leaf leaf, Middle middle)
            {
                Leaf = leaf;
                Middle = middle;
            }
        }

        public class ServiceConsumer
        {
            public readonly IService Service;

            public ServiceConsumer(IService service)
            {
                Service = service;
            }
        }

        public class FourDependencies
        {
            public FourDependencies(Leaf leaf, Middle middle, Top top, IService service) { }
        }

        ReflectionBasedDependencyResolver _dependencyResolver = null!;

        [SetUp]
        public void Setup()
        {
            _dependencyResolver = new ReflectionBasedDependencyResolver();
        }

        [Test]
        public void Build_InjectsTheRegisteredInstance_WhenATypeDependsOnIt()
        {
            var leaf = new Leaf();

            _dependencyResolver.RegisterInstance(leaf);
            _dependencyResolver.RegisterType<Middle>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<Middle>().Leaf, Is.SameAs(leaf));
        }

        [Test]
        public void Build_SharesOneInstanceOfARegistration_BetweenEveryDependent()
        {
            _dependencyResolver.RegisterType<Leaf>();
            _dependencyResolver.RegisterType<Middle>();
            _dependencyResolver.RegisterType<Top>();

            var result = _dependencyResolver.Build();

            var leaf = result.Get<Leaf>();
            Assert.That(result.Get<Middle>().Leaf, Is.SameAs(leaf));
            Assert.That(result.Get<Top>().Leaf, Is.SameAs(leaf));
            Assert.That(result.Get<Top>().Middle, Is.SameAs(result.Get<Middle>()));
        }

        [Test]
        public void Build_PassesResolvedInstancesToAFactory()
        {
            var leaf = new Leaf();
            Middle? built = null;

            _dependencyResolver.RegisterInstance(leaf);
            _dependencyResolver.RegisterFactory<Middle, Leaf>(l => built = new Middle(l));

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<Middle>(), Is.SameAs(built));
            Assert.That(result.Get<Middle>().Leaf, Is.SameAs(leaf));
        }

        [Test]
        public void Build_InjectsARegistrationThroughTheTypeItIsRegisteredAs()
        {
            _dependencyResolver.RegisterType<Service>().As<IService>();
            _dependencyResolver.RegisterType<ServiceConsumer>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<ServiceConsumer>().Service, Is.SameAs(result.Get<Service>()));
        }

        [Test]
        public void Build_ResolvesAnInstanceRegistration_ThroughTheTypeItIsRegisteredAs()
        {
            var service = new Service();

            _dependencyResolver.RegisterInstance(service).As<IService>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<IService>(), Is.SameAs(service));
        }

        [Test]
        public void Build_ResolvesAFactoryRegistration_ThroughTheTypeItIsRegisteredAs()
        {
            _dependencyResolver.RegisterFactory<Service>(() => new Service()).As<IService>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<IService>(), Is.SameAs(result.Get<Service>()));
        }

        [Test]
        public void Build_ResolvesAFactory_WhenItTakesSeveralDependencies()
        {
            _dependencyResolver.RegisterType<Leaf>();
            _dependencyResolver.RegisterType<Middle>();
            _dependencyResolver.RegisterType<Top>();
            _dependencyResolver.RegisterType<Service>().As<IService>();
            _dependencyResolver.RegisterFactory<FourDependencies, Leaf, Middle, Top, IService>(
                (leaf, middle, top, service) => new FourDependencies(leaf, middle, top, service)
            );

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<FourDependencies>(), Is.Not.Null);
        }

        [Test]
        public void Build_UsesTheLastRegistration_WhenATypeIsRegisteredTwice()
        {
            var first = new Leaf();
            var second = new Leaf();

            _dependencyResolver.RegisterInstance(first);
            _dependencyResolver.RegisterInstance(second);

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<Leaf>(), Is.SameAs(second));
        }

        [Test]
        public void Build_ReturnsAnEmptyRepository_WhenNothingIsRegistered()
        {
            var result = _dependencyResolver.Build();

            Assert.That(result.TryGet<Leaf>(out _), Is.False);
        }

        [Test]
        public void TryGet_ReturnsTrueAndTheInstance_WhenTypeIsRegistered()
        {
            var leaf = new Leaf();
            _dependencyResolver.RegisterInstance(leaf);

            var result = _dependencyResolver.Build();

            Assert.That(result.TryGet<Leaf>(out var found), Is.True);
            Assert.That(found, Is.SameAs(leaf));
        }

        [Test]
        public void Build_ReturnsANewInstanceOnEveryBuild_WhenRegisteredByType()
        {
            _dependencyResolver.RegisterType<Leaf>();

            var first = _dependencyResolver.Build();
            var second = _dependencyResolver.Build();

            Assert.That(first.Get<Leaf>(), Is.Not.SameAs(second.Get<Leaf>()));
        }
    }
}
