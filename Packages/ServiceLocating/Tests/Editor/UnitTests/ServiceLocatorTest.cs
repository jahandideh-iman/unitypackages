using NUnit.Framework;

namespace Arman.ServiceLocating.Tests
{
    public class ServiceLocatorTest
    {
        interface IServiceA { }

        interface IServiceB { }

        class ServiceA : IServiceA { }

        class OtherServiceA : IServiceA { }

        class ServiceB : IServiceB { }

        // The locator is a process-wide singleton, so every test starts and ends without one.
        [SetUp]
        public void Setup()
        {
            ServiceLocator.Clear();
            ServiceLocator.Init();
        }

        [TearDown]
        public void Teardown()
        {
            ServiceLocator.Clear();
        }

        [Test]
        public void InitializingShouldMakeTheLocatorInitialized()
        {
            Assert.That(ServiceLocator.IsInited(), Is.True);
        }

        [Test]
        public void ClearingShouldMakeTheLocatorUninitialized()
        {
            ServiceLocator.Clear();

            Assert.That(ServiceLocator.IsInited(), Is.False);
        }

        [Test]
        public void InitializingAgainShouldKeepTheRegisteredServices()
        {
            var service = new ServiceA();
            ServiceLocator.Register<IServiceA, ServiceA>(service);

            ServiceLocator.Init();

            Assert.That(ServiceLocator.Find<IServiceA>(), Is.SameAs(service));
        }

        [Test]
        public void ClearingAndInitializingShouldForgetTheRegisteredServices()
        {
            ServiceLocator.Register<IServiceA, ServiceA>(new ServiceA());

            ServiceLocator.Clear();
            ServiceLocator.Init();

            Assert.That(() => ServiceLocator.Find<IServiceA>(), Throws.Exception);
        }

        [Test]
        public void FindingShouldReturnTheRegisteredServices()
        {
            var serviceA = new ServiceA();
            var serviceB = new ServiceB();

            ServiceLocator.Register<IServiceA, ServiceA>(serviceA);
            ServiceLocator.Register<IServiceB, ServiceB>(serviceB);

            Assert.That(ServiceLocator.Find<IServiceA>(), Is.SameAs(serviceA));
            Assert.That(ServiceLocator.Find<IServiceB>(), Is.SameAs(serviceB));
        }

        [Test]
        public void FindingShouldAlsoMatchTheImplementationType()
        {
            var service = new ServiceA();

            ServiceLocator.Register<IServiceA, ServiceA>(service);

            Assert.That(ServiceLocator.Find<ServiceA>(), Is.SameAs(service));
        }

        [Test]
        public void FindingAnUnregisteredServiceShouldThrow()
        {
            ServiceLocator.Register<IServiceA, ServiceA>(new ServiceA());

            Assert.That(() => ServiceLocator.Find<IServiceB>(), Throws.Exception);
        }

        [Test]
        public void FindingShouldReturnTheFirstRegisteredMatch()
        {
            var first = new ServiceA();

            ServiceLocator.Register<IServiceA, ServiceA>(first);
            ServiceLocator.Register<IServiceA, OtherServiceA>(new OtherServiceA());

            Assert.That(ServiceLocator.Find<IServiceA>(), Is.SameAs(first));
        }

        [Test]
        public void UnregisteringShouldRemoveTheService()
        {
            var serviceB = new ServiceB();
            ServiceLocator.Register<IServiceA, ServiceA>(new ServiceA());
            ServiceLocator.Register<IServiceB, ServiceB>(serviceB);

            ServiceLocator.UnRegister<IServiceA>();

            Assert.That(() => ServiceLocator.Find<IServiceA>(), Throws.Exception);
            Assert.That(ServiceLocator.Find<IServiceB>(), Is.SameAs(serviceB));
        }

        [Test]
        public void ReplacingShouldSwapTheServiceForTheNewOne()
        {
            var replacement = new OtherServiceA();
            ServiceLocator.Register<IServiceA, ServiceA>(new ServiceA());

            ServiceLocator.Replace<IServiceA, OtherServiceA>(replacement);

            Assert.That(ServiceLocator.Find<IServiceA>(), Is.SameAs(replacement));
        }
    }
}
