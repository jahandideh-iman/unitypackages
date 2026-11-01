using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Arman.DevelopmentConsole.Tests
{
    public class CommandInfoTest
    {
        // Commands are static methods, so what they did is kept in static fields and reset per test.
        static class FakeCommands
        {
            public static int RunCount;
            public static object[] ReceivedInputs = new object[0];

            public static void Run()
            {
                RunCount++;
            }

            public static void RunWith(int number, float ratio, string text)
            {
                ReceivedInputs = new object[] { number, ratio, text };
            }
        }

        [SetUp]
        public void Setup()
        {
            FakeCommands.RunCount = 0;
            FakeCommands.ReceivedInputs = new object[0];
        }

        [Test]
        public void ShouldKeepItsNameAndMethod()
        {
            var method = Method(nameof(FakeCommands.Run));

            var command = new CommandInfo("Run", method);

            Assert.That(command.Name, Is.EqualTo("Run"));
            Assert.That(command.MethodInfo, Is.SameAs(method));
        }

        [Test]
        public void InvokingShouldRunTheMethod()
        {
            var command = new CommandInfo("Run", Method(nameof(FakeCommands.Run)));

            command.Invoke();

            Assert.That(FakeCommands.RunCount, Is.EqualTo(1));
        }

        [Test]
        public void InvokingWithInputsShouldPassThemToTheMethod()
        {
            var command = new CommandInfo("RunWith", Method(nameof(FakeCommands.RunWith)));

            command.Invoke(new object[] { 3, 0.5f, "text" });

            Assert.That(FakeCommands.ReceivedInputs, Is.EqualTo(new object[] { 3, 0.5f, "text" }));
        }

        [Test]
        public void AMethodWithoutParametersShouldHaveNoInput()
        {
            var command = new CommandInfo("Run", Method(nameof(FakeCommands.Run)));

            Assert.That(command.HasNoInput(), Is.True);
        }

        [Test]
        public void AMethodWithParametersShouldHaveInput()
        {
            var command = new CommandInfo("RunWith", Method(nameof(FakeCommands.RunWith)));

            Assert.That(command.HasNoInput(), Is.False);
        }

        [Test]
        public void ANewCommandShouldHaveNoShortcut()
        {
            var command = new CommandInfo("Run", Method(nameof(FakeCommands.Run)));

            Assert.That(command.KeyCodes, Is.Empty);
        }

        [Test]
        public void SettingAShortcutShouldKeepItsKeys()
        {
            var command = new CommandInfo("Run", Method(nameof(FakeCommands.Run)));
            var keys = new[] { KeyCode.LeftShift, KeyCode.D };

            command.SetShortcut(keys);

            Assert.That(command.KeyCodes, Is.EqualTo(keys));
        }

        static MethodInfo Method(string name)
        {
            return typeof(FakeCommands).GetMethod(name)!;
        }
    }
}
