using UnityEngine;

namespace ShikiShiro
{
    public sealed class TpsCamera : MonoBehaviour
    {
        private PlayerMotor _target;
        private float _shakeTime;
        private Vector3 _shake;

        public Camera UnityCamera { get; private set; }

        public void Initialize(PlayerMotor target, GameConfig config)
        {
            _target = target;
            UnityCamera = gameObject.AddComponent<Camera>();
            UnityCamera.fieldOfView = 75f;
            UnityCamera.nearClipPlane = 0.03f;
            UnityCamera.farClipPlane = 650f;
            UnityCamera.backgroundColor = new Color(0.62f, 0.76f, 0.92f);
            UnityCamera.clearFlags = CameraClearFlags.SolidColor;
            int ui = LayerMask.NameToLayer("UI");
            int view = LayerMask.NameToLayer("ViewModel");
            int mask = ~0;
            if (ui >= 0)
            {
                mask &= ~(1 << ui);
            }

            if (view >= 0)
            {
                mask &= ~(1 << view);
            }

            UnityCamera.cullingMask = mask;

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].gameObject != gameObject)
                {
                    listeners[i].enabled = false;
                }
            }

            AudioListener mine = GetComponent<AudioListener>();
            if (mine == null)
            {
                mine = gameObject.AddComponent<AudioListener>();
            }

            mine.enabled = true;
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            target.HideBody();
        }

        public void Shake(float amount, float duration)
        {
            _shakeTime = duration;
            _shake = UnityEngine.Random.insideUnitSphere * amount;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Quaternion rot = Quaternion.Euler(_target.Pitch, _target.Yaw, 0f);
            Vector3 pos = _target.Head.position;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                pos += _shake * (_shakeTime / 0.12f);
            }

            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
