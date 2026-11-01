using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using static Arman.UIManagement.Tests.UnityComponentCreationExtensions;

namespace Arman.UIManagement.Tests
{
    public class PanelTests
    {
        private static readonly Color _originalColor = new Color(0.2f, 0.4f, 0.6f, 0.8f);

        private Panel _panel = null!;
        private Image _backgroundImage = null!;

        [SetUp]
        public void SetUp()
        {
            CreateGameObject(
                "Panel",
                go =>
                {
                    _backgroundImage = go.AddComponent<Image>();
                    _backgroundImage.color = _originalColor;
                    go.AddComponent<Panel>(preAwake: panel =>
                        {
                            SetField(panel, "BackgroundImage", _backgroundImage);
                        })
                        .Out(out _panel);
                }
            );
        }

        [TearDown]
        public void Teardown()
        {
            CleanUpScene();
        }

        [Test]
        public void SetAlpha_ChangesOnlyTheAlphaOfTheBackground()
        {
            _panel.SetAlpha(0.25f);

            var color = _backgroundImage.color;
            Assert.That(color.a, Is.EqualTo(0.25f));
            Assert.That(color.r, Is.EqualTo(_originalColor.r));
            Assert.That(color.g, Is.EqualTo(_originalColor.g));
            Assert.That(color.b, Is.EqualTo(_originalColor.b));
        }

        [Test]
        public void RestoreAlpha_ReturnsTheBackgroundToTheAlphaItHadOnAwake()
        {
            _panel.SetAlpha(0.25f);

            _panel.RestoreAlpha();

            Assert.That(_backgroundImage.color.a, Is.EqualTo(_originalColor.a));
        }

        [Test]
        public void SetVisible_ShowsAndHidesThePanel()
        {
            _panel.SetVisible(false);
            Assert.That(_panel.gameObject.activeSelf, Is.False);

            _panel.SetVisible(true);
            Assert.That(_panel.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void APanel_CarriesTheComponentsAWindowRequires()
        {
            Assert.That(_panel.GetComponent<CanvasGroup>() != null, Is.True);
            Assert.That(_panel.GetComponent<Canvas>() != null, Is.True);
            Assert.That(_panel.GetComponent<GraphicRaycaster>() != null, Is.True);
        }

        private static void SetField(object target, string name, object value)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo? field = null;
            for (var type = target.GetType(); field == null && type != null; type = type.BaseType)
                field = type.GetField(name, flags);
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name} has no field '{name}'");
            field!.SetValue(target, value);
        }
    }
}
