using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Arman.DevelopmentConsole.Tests
{
    public class DevelopmentConsolePanelTest
    {
        [AttributeUsage(AttributeTargets.All)]
        public class FakeMarkerAttribute : Attribute { }

        // Deliberately not a DevelopmentOptionsDefinition: the panel discovers those across every
        // loaded assembly, and test options must not show up in a real console.
        public class FakeOptions
        {
            [DevOption("Group", "Command")]
            [FakeMarker]
            public static void Command() { }

            [DevOption("Group", "Shortcut Command")]
            [ShortCut(KeyCode.LeftShift, KeyCode.A)]
            public static void ShortcutCommand() { }

            [ShortCut(KeyCode.LeftControl)]
            public static void ShortcutOnly() { }

            public static void Plain() { }

            [FakeMarker]
            public static int Field;
        }

        [Test]
        public void GettingMethodsWithAnAttributeShouldReturnOnlyTheMethodsCarryingIt()
        {
            var methods = DevelopmentConsolePanel.GetMethodsWithAttribute(
                typeof(FakeOptions),
                typeof(DevOptionAttribute)
            );

            Assert.That(
                methods.Select(m => m.Name),
                Is.EquivalentTo(
                    new[] { nameof(FakeOptions.Command), nameof(FakeOptions.ShortcutCommand) }
                )
            );
        }

        [Test]
        public void GettingMethodsWithTheShortcutAttributeShouldReturnEveryShortcutMethod()
        {
            var methods = DevelopmentConsolePanel.GetMethodsWithAttribute(
                typeof(FakeOptions),
                typeof(ShortCutAttribute)
            );

            Assert.That(
                methods.Select(m => m.Name),
                Is.EquivalentTo(
                    new[] { nameof(FakeOptions.ShortcutCommand), nameof(FakeOptions.ShortcutOnly) }
                )
            );
        }

        [Test]
        public void GettingMembersWithAnAttributeShouldIncludeFieldsAndMethods()
        {
            var members = DevelopmentConsolePanel.GetMembersWithAttribute(
                typeof(FakeOptions),
                typeof(FakeMarkerAttribute)
            );

            Assert.That(
                members.Select(m => m.Name),
                Is.EquivalentTo(new[] { nameof(FakeOptions.Command), nameof(FakeOptions.Field) })
            );
        }

        [Test]
        public void TheAttributesShouldKeepTheValuesTheyWereDeclaredWith()
        {
            var method = typeof(FakeOptions).GetMethod(nameof(FakeOptions.ShortcutCommand))!;

            var devOption = (DevOptionAttribute)
                method.GetCustomAttributes(typeof(DevOptionAttribute), false)[0];
            var shortcut = (ShortCutAttribute)
                method.GetCustomAttributes(typeof(ShortCutAttribute), false)[0];

            Assert.That(devOption.Group, Is.EqualTo("Group"));
            Assert.That(devOption.CommandName, Is.EqualTo("Shortcut Command"));
            Assert.That(shortcut.KeyCodes, Is.EqualTo(new[] { KeyCode.LeftShift, KeyCode.A }));
        }
    }
}
