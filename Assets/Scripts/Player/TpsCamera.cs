using UnityEngine;

namespace ShikiShiro
{
    public sealed class TpsCamera : MonoBehaviour
    {
        private PlayerMotor _target;
        private float _shakeTime;
        private float _shakeDuration;
        private Vector3 _shake;
        private float _kickTime;
        private float _kickDuration;
        private Vector3 _kickPos;
        private float _kickRoll;
        private float _kickFov;
        private const float BaseFov = 75f;

        public Camera UnityCamera { get; private set; }

        public void Initialize(PlayerMotor target, GameConfig config)
        {
            _target = target;
            UnityCamera = gameObject.AddComponent<Camera>();
            UnityCamera.fieldOfView = BaseFov;
            UnityCamera.nearClipPlane = 0.12f;
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
            _shakeDuration = Mathf.Max(0.04f, duration);
            _shakeTime = _shakeDuration;
            _shake = UnityEngine.Random.insideUnitSphere * amount;
        }

        public void BiteKick(Vector3 incoming, float intensity)
        {
            intensity = Mathf.Clamp(intensity, 0.45f, 1.8f);
            Vector3 dir = incoming.sqrMagnitude > 0.0001f ? incoming.normalized : transform.forward;
            Vector3 local = transform.InverseTransformDirection(dir);
            _kickPos = new Vector3(-local.x * 0.16f, -0.06f - local.y * 0.08f, -0.12f) * intensity;
            _kickRoll = Mathf.Clamp(-local.x * 18f * intensity, -20f, 20f);
            _kickFov = 9f * intensity;
            _kickDuration = 0.34f;
            _kickTime = _kickDuration;
            Shake(0.2f * intensity, 0.26f);
            _target?.AddHitPunch(-local.x * 5.5f * intensity, 10f * intensity);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            float roll = 0f;
            Vector3 pos = _target.Head.position;
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float w = Mathf.Clamp01(_shakeTime / _shakeDuration);
                pos += _shake * w;
            }

            if (_kickTime > 0f)
            {
                _kickTime -= Time.deltaTime;
                float k = Mathf.Clamp01(_kickTime / _kickDuration);
                float t = 1f - k;
                float chomp = Mathf.Exp(-t * 7f) + 0.28f * Mathf.Exp(-(t - 0.11f) * (t - 0.11f) * 180f);
                pos += _kickPos * chomp;
                roll = _kickRoll * chomp;
                UnityCamera.fieldOfView = BaseFov + _kickFov * chomp;
            }
            else
            {
                UnityCamera.fieldOfView = Mathf.MoveTowards(UnityCamera.fieldOfView, BaseFov, Time.deltaTime * 40f);
            }

            Quaternion rot = Quaternion.Euler(_target.Pitch, _target.Yaw, roll);
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
