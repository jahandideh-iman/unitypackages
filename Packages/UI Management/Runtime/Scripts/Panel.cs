using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.UIManagement
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Panel : Window
    {
        [FormerlySerializedAs("backgroundImage")]
        [SerializeField]
        Image BackgroundImage = default;

        float _originalAlpha;

        private void Awake()
        {
            _originalAlpha = BackgroundImage.color.a;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetAlpha(float opacity)
        {
            var color = BackgroundImage.color;
            color.a = opacity;
            BackgroundImage.color = color;
        }

        public void RestoreAlpha()
        {
            SetAlpha(_originalAlpha);
        }
    }
}
