using Arman.Foundation.Core.ConfigurationManagement;
using NUnit.Framework;


namespace Arman.Tests.Foundation.Core.ConfigurationManagement
{

    class TypeA { }
    class TypeB { }


    class FakeMultiConfigurerAB : Configurer<TypeA>, Configurer<TypeB>
    {
        public void Configure(TypeA entity)
        {
            
        }

        public void Configure(TypeB entity)
        {
            
        }

        public void RegisterSelf(ConfigurationManager manager)
        {
            
        }
    }

    class FakeConfigurer<T> : Configurer<T>
    {

        public bool configurationCalled = false;

        public void Configure(T entity)
        {
            configurationCalled = true;
        }

        public void RegisterSelf(ConfigurationManager manager)
        {
            
        }
    }


    public class BasicConfigurationManagerTest 
    {

        BasicConfigurationManager configManager;

        FakeConfigurer<TypeA> configurerA;
        FakeConfigurer<TypeB> configurerB;


        [SetUp]
        public void Setup()
        {
            configManager = new BasicConfigurationManager();

            configurerA = new FakeConfigurer<TypeA>();
            configurerB = new FakeConfigurer<TypeB>();
        }

        [Test]
        public void HasTheRegisteredConfigurers()
        {
            
            configManager.Register(configurerA);
            configManager.Register(configurerB);

            Assert.That(configManager.Contains(configurerA));
            Assert.That(configManager.Contains(configurerB));
        }

        [Test]
        public void ConfigurersCanBeRemoved()
        {
            configManager.Register(configurerA);
            configManager.Register(configurerB);

            var removedA = configManager.RemoveConfigurer<TypeA>();
            var removedB = configManager.RemoveConfigurer<TypeB>();

            Assert.That(removedA, Is.SameAs(configurerA));
            Assert.That(removedB, Is.SameAs(configurerB));
            Assert.That(configManager.Contains(configurerA), Is.False);
            Assert.That(configManager.Contains(configurerB), Is.False);
        }

        [Test]
        public void DelegatesTheConfigurationToTheCorrectConfigurer()
        {
            configManager.Register(configurerA);
            configManager.Register(configurerB);

            configManager.Configure(new TypeA());

            Assert.That(configurerA.configurationCalled, Is.True);
            Assert.That(configurerB.configurationCalled, Is.False);
        }

        [Test]
        public void MultiConfigurersCanBePartialyRemoved()
        {
            var configurerAB = new FakeMultiConfigurerAB();
   
            configManager.Register<TypeA>(configurerAB);
            configManager.Register<TypeB>(configurerAB);

            var removedA = configManager.RemoveConfigurer<TypeA>();

            Assert.That(removedA, Is.SameAs(configurerAB));
            Assert.That(configManager.FindConfigurer<TypeA>(), Is.Null);
            Assert.That(configManager.FindConfigurer<TypeB>(), Is.SameAs(configurerAB));
        }
    }
}