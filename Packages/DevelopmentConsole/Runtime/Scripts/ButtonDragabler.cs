using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    [RequireComponent(typeof(Button))]
    public class ButtonDragabler : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        public float dragThreshold;

        Button button;
        bool mustCheckForDrag = false;
        bool isDragging = false;

        void Awake()
        {
            button = this.transform.GetComponent<Button>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            mustCheckForDrag = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mustCheckForDrag)
            {
                if (Vector2.Distance(eventData.position, eventData.pressPosition) >= dragThreshold)
                {
                    isDragging = true;
                    mustCheckForDrag = false;
                    button.enabled = false;
                }
            }

            if (isDragging)
            {
                this.transform.position = eventData.position;
                eventData.Use();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            mustCheckForDrag = false;
            button.enabled = true;
            ;
        }
    }
}
