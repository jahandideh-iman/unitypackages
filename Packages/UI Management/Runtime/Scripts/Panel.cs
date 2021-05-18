using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.Presentation.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Panel : Window
    {
        
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

    }
}