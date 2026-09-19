using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShikiShiro
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _radius = 90f;

        public Vector2 Value { get; private set; }

        private RectTransform _root;
        private Vector2 _pointerStart;
        private bool _held;

        public void Configure(RectTransform handle, float radius)
        {
            _handle = handle;
            _radius = radius;
            _root = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, eventData.position, eventData.pressEventCamera, out _pointerStart);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_held)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Vector2 delta = local - _pointerStart;
            Vector2 clamped = Vector2.ClampMagnitude(delta, _radius);
            if (_handle != null)
            {
                _handle.anchoredPosition = clamped;
            }

            Value = clamped / _radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _held = false;
            Value = Vector2.zero;
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
