
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arman.Presentation
{
    [RequireComponent(typeof(Button))]
    public class ButtonDragabler : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        public float dragThreshold;

        Button button;
        bool mustCheckForDrag = false;
        bool isDraging = false;

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
            if(mustCheckForDrag)
            {
                Debug.Log(Vector2.Distance(eventData.position, eventData.pressPosition));
                if (Vector2.Distance(eventData.position, eventData.pressPosition) >= dragThreshold)
                {
                    isDraging = true;
                    mustCheckForDrag = false;
                    button.enabled = false;
                }

            }

            if (isDraging)
            {
                this.transform.position = eventData.position;
                eventData.Use();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDraging = false;
            mustCheckForDrag = false;
            button.enabled = true; ;
        }

    }
}