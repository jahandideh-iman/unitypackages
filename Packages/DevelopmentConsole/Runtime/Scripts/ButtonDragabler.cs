using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    [RequireComponent(typeof(Button))]
    public class ButtonDragabler : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        [FormerlySerializedAs("dragThreshold")]
        public float DragThreshold;

        Button _button;
        bool _mustCheckForDrag = false;
        bool _isDragging = false;

        void Awake()
        {
            _button = this.transform.GetComponent<Button>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _mustCheckForDrag = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_mustCheckForDrag)
            {
                if (Vector2.Distance(eventData.position, eventData.pressPosition) >= DragThreshold)
                {
                    _isDragging = true;
                    _mustCheckForDrag = false;
                    _button.enabled = false;
                }
            }

            if (_isDragging)
            {
                this.transform.position = eventData.position;
                eventData.Use();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _mustCheckForDrag = false;
            _button.enabled = true;
            ;
        }
    }
}
