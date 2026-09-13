using NUnit.Framework;

namespace Arman.DependencyResolution.Tests
{

    public class DependencyResolverTest_BasicRegistration
    {
        public class A
        {
            public static A Create() => new A();
        }

        public class B
        {
            public B(A a) { }
            public static B Create(A a) => new B(a);
        }

        public class C
        {
            public C(A a, B b) { }
            public static C Create(A a, B b) => new C(a, b);
        }

        ReflectionBasedDependencyResolver _dependencyResolver = null!;

        [SetUp]
        public void Setup()
        {
            _dependencyResolver = new ReflectionBasedDependencyResolver();
        }

        [Test]
        public void Build_ResolvesRegistrations_WhenRegisteredFromInstance()
        {
            var a = new A();
            var b = new B(a);
            var c = new C(a, b);

            _dependencyResolver.RegisterInstance(a);
            _dependencyResolver.RegisterInstance(b);
            _dependencyResolver.RegisterInstance(c);

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<A>(), Is.SameAs(a));
            Assert.That(result.Get<B>(), Is.SameAs(b));
            Assert.That(result.Get<C>(), Is.SameAs(c));
        }

        [Test]
        public void Build_ResolvesRegistrations_WhenRegisteredByType()
        {
            _dependencyResolver.RegisterType<C>();
            _dependencyResolver.RegisterType<B>();
            _dependencyResolver.RegisterType<A>();

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<A>(), Is.Not.Null);
            Assert.That(result.Get<B>(), Is.Not.Null);
            Assert.That(result.Get<C>(), Is.Not.Null);
        }

        [Test]
        public void Build_ResolvesRegistrations_WhenRegisteredByFactory()
        {
            _dependencyResolver.RegisterFactory<C, A, B>(C.Create);
            _dependencyResolver.RegisterFactory<B, A>(B.Create);
            _dependencyResolver.RegisterFactory<A>(A.Create);

            var result = _dependencyResolver.Build();

            Assert.That(result.Get<A>(), Is.Not.Null);
            Assert.That(result.Get<B>(), Is.Not.Null);
            Assert.That(result.Get<C>(), Is.Not.Null);
        }
    }
}