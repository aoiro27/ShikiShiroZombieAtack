using UnityEngine;

namespace ShikiShiro
{
    public sealed class TpsCamera : MonoBehaviour
    {
        private PlayerMotor _target;
        private GameConfig _config;
        private Transform _cam;
        private Vector3 _shake;
        private float _shakeTime;

        public Camera UnityCamera { get; private set; }

        public void Initialize(PlayerMotor target, GameConfig config)
        {
            _target = target;
            _config = config;
            UnityCamera = gameObject.AddComponent<Camera>();
            UnityCamera.fieldOfView = 62f;
            UnityCamera.nearClipPlane = 0.08f;
            UnityCamera.farClipPlane = 180f;
            UnityCamera.backgroundColor = new Color(0.015f, 0.02f, 0.03f);
            UnityCamera.clearFlags = CameraClearFlags.SolidColor;
            gameObject.AddComponent<AudioListener>();
            _cam = transform;
            int ui = LayerMask.NameToLayer("UI");
            if (ui >= 0)
            {
                UnityCamera.cullingMask = ~(1 << ui);
            }
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
            Vector3 pivot = _target.Head.position;
            Vector3 desired = pivot - rot * Vector3.forward * _config.CameraDistance + Vector3.up * 0.15f;
            Vector3 dir = desired - pivot;
            float dist = dir.magnitude;
            if (Physics.SphereCast(pivot, _config.CameraCollisionRadius, dir.normalized, out RaycastHit hit, dist, ~LayerMask.GetMask("Player", "Zombie", "UI"), QueryTriggerInteraction.Ignore))
            {
                desired = pivot + dir.normalized * Mathf.Max(0.35f, hit.distance - 0.12f);
            }

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                desired += _shake * (_shakeTime / 0.12f);
            }

            _cam.position = desired;
            _cam.rotation = rot;
        }
    }
}
