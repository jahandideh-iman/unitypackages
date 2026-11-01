using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arman.ConfigurationManagement.Tests
{
    public class DynamicConfigurerTest
    {
        DynamicConfigurer<TypeA> _configurer = null!;
        List<string> _executed = null!;

        [SetUp]
        public void Setup()
        {
            _configurer = new DynamicConfigurer<TypeA>();
            _executed = new List<string>();
        }

        [Test]
        public void ConfiguringShouldRunTheAddedActionsInOrderOnTheEntity()
        {
            var entity = new TypeA();
            var configured = new List<TypeA>();

            _configurer.AddConfigAction(e =>
            {
                _executed.Add("first");
                configured.Add(e);
            });
            _configurer.AddConfigAction(e =>
            {
                _executed.Add("second");
                configured.Add(e);
            });

            _configurer.Configure(entity);

            Assert.That(_executed, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(configured, Is.EqualTo(new[] { entity, entity }));
        }

        [Test]
        public void AThrowingActionShouldBeLoggedAndNotStopTheRemainingActions()
        {
            _configurer.AddConfigAction(e => throw new InvalidOperationException("action failed"));
            _configurer.AddConfigAction(e => _executed.Add("after"));

            LogAssert.Expect(LogType.Error, new Regex("action failed"));

            _configurer.Configure(new TypeA());

            Assert.That(_executed, Is.EqualTo(new[] { "after" }));
        }

        [Test]
        public void RegisteringSelfShouldRegisterTheConfigurerForItsType()
        {
            var manager = new ConfigurationManager();

            _configurer.RegisterSelf(manager);

            Assert.That(manager.FindConfigurer<TypeA>(), Is.SameAs(_configurer));
        }
    }
}
