using NUnit.Framework;

namespace Arman.ConfigurationManagement.Tests
{
    public class CompositeConfigurerTest
    {
        CompositeConfigurer<TypeA> _composite = null!;

        [SetUp]
        public void Setup()
        {
            _composite = new CompositeConfigurer<TypeA>();
        }

        [Test]
        public void ConfiguringShouldDelegateTheEntityToAllAddedConfigurers()
        {
            var configurer1 = new FakeConfigurer<TypeA>();
            var configurer2 = new FakeConfigurer<TypeA>();

            _composite.AddConfigurer(configurer1);
            _composite.AddConfigurer(configurer2);

            var entity = new TypeA();
            _composite.Configure(entity);

            Assert.That(configurer1.ConfiguredEntity, Is.SameAs(entity));
            Assert.That(configurer2.ConfiguredEntity, Is.SameAs(entity));
        }

        [Test]
        public void ConfiguringWithoutConfigurersShouldDoNothing()
        {
            Assert.That(() => _composite.Configure(new TypeA()), Throws.Nothing);
        }

        [Test]
        public void RegisteringSelfShouldRegisterTheCompositeForItsType()
        {
            var manager = new ConfigurationManager();

            _composite.RegisterSelf(manager);

            Assert.That(manager.FindConfigurer<TypeA>(), Is.SameAs(_composite));
        }

        [Test]
        public void ConfiguringThroughAManagerShouldReachTheAddedConfigurers()
        {
            var manager = new ConfigurationManager();
            var configurer = new FakeConfigurer<TypeA>();
            _composite.AddConfigurer(configurer);
            _composite.RegisterSelf(manager);

            var entity = new TypeA();
            manager.Configure(entity);

            Assert.That(configurer.ConfiguredEntity, Is.SameAs(entity));
        }
    }
}
