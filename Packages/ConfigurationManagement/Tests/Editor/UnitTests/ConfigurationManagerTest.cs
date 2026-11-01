using NUnit.Framework;

namespace Arman.ConfigurationManagement.Tests
{
    class TypeA { }

    class TypeB { }

    class FakeMultiConfigurerAB : IConfigurer<TypeA>, IConfigurer<TypeB>
    {
        public void Configure(TypeA entity) { }

        public void Configure(TypeB entity) { }

        public void RegisterSelf(IConfigurationManager manager) { }
    }

    class FakeConfigurer<T> : IConfigurer<T>
    {
        public bool ConfigurationCalled = false;
        public T ConfiguredEntity = default!;

        public void Configure(T entity)
        {
            ConfigurationCalled = true;
            ConfiguredEntity = entity;
        }

        public void RegisterSelf(IConfigurationManager manager) { }
    }

    public class ConfigurationManagerTest
    {
        ConfigurationManager _configManager;

        FakeConfigurer<TypeA> _configurerA;
        FakeConfigurer<TypeB> _configurerB;

        [SetUp]
        public void Setup()
        {
            _configManager = new ConfigurationManager();

            _configurerA = new FakeConfigurer<TypeA>();
            _configurerB = new FakeConfigurer<TypeB>();
        }

        [Test]
        public void HasTheRegisteredConfigurers()
        {
            _configManager.Register(_configurerA);
            _configManager.Register(_configurerB);

            Assert.That(_configManager.Contains(_configurerA));
            Assert.That(_configManager.Contains(_configurerB));
        }

        [Test]
        public void ConfigurersCanBeRemoved()
        {
            _configManager.Register(_configurerA);
            _configManager.Register(_configurerB);

            var removedA = _configManager.RemoveConfigurer<TypeA>();
            var removedB = _configManager.RemoveConfigurer<TypeB>();

            Assert.That(removedA, Is.SameAs(_configurerA));
            Assert.That(removedB, Is.SameAs(_configurerB));
            Assert.That(_configManager.Contains(_configurerA), Is.False);
            Assert.That(_configManager.Contains(_configurerB), Is.False);
        }

        [Test]
        public void DelegatesTheConfigurationToTheCorrectConfigurer()
        {
            _configManager.Register(_configurerA);
            _configManager.Register(_configurerB);

            _configManager.Configure(new TypeA());

            Assert.That(_configurerA.ConfigurationCalled, Is.True);
            Assert.That(_configurerB.ConfigurationCalled, Is.False);
        }

        [Test]
        public void MultiConfigurersCanBePartiallyRemoved()
        {
            var configurerAB = new FakeMultiConfigurerAB();

            _configManager.Register<TypeA>(configurerAB);
            _configManager.Register<TypeB>(configurerAB);

            var removedA = _configManager.RemoveConfigurer<TypeA>();

            Assert.That(removedA, Is.SameAs(configurerAB));
            Assert.That(_configManager.FindConfigurer<TypeA>(), Is.Null);
            Assert.That(_configManager.FindConfigurer<TypeB>(), Is.SameAs(configurerAB));
        }

        [Test]
        public void ConfiguringShouldGiveTheTargetToTheConfigurer()
        {
            _configManager.Register(_configurerA);

            var target = new TypeA();
            _configManager.Configure(target);

            Assert.That(_configurerA.ConfiguredEntity, Is.SameAs(target));
        }

        [Test]
        public void FindingAnUnregisteredConfigurerShouldReturnNull()
        {
            Assert.That(_configManager.FindConfigurer<TypeA>(), Is.Null);
        }

        [Test]
        public void RemovingAnUnregisteredConfigurerShouldReturnNull()
        {
            _configManager.Register(_configurerB);

            Assert.That(_configManager.RemoveConfigurer<TypeA>(), Is.Null);
            Assert.That(_configManager.FindConfigurer<TypeB>(), Is.SameAs(_configurerB));
        }

        [Test]
        public void RegisteringAConfigurerForAnAlreadyRegisteredTypeShouldReplaceIt()
        {
            var replacement = new FakeConfigurer<TypeA>();

            _configManager.Register(_configurerA);
            _configManager.Register(replacement);

            _configManager.Configure(new TypeA());

            Assert.That(_configManager.FindConfigurer<TypeA>(), Is.SameAs(replacement));
            Assert.That(replacement.ConfigurationCalled, Is.True);
            Assert.That(_configurerA.ConfigurationCalled, Is.False);
        }
    }
}
