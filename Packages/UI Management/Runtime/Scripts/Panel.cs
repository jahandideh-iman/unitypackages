using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Panel : Window
    {
        [SerializeField] Image backgroundImage = default;

        float originalAlpha;

        private void Awake()
        {
            originalAlpha = backgroundImage.color.a;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetAlpha(float opacity)
        {
            var color = backgroundImage.color;
            color.a = opacity;
            backgroundImage.color = color;
        }

        public void RestoreAlpha()
        {
            SetAlpha(originalAlpha);
        }
    }
}