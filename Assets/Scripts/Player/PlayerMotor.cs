using UnityEngine;

namespace ShikiShiro
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Transform Head { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = 12f;

        private CharacterController _controller;
        private GameInput _input;
        private GameConfig _config;
        private float _verticalVelocity;
        private float _recoilPitch;

        public void Initialize(GameInput input, GameConfig config)
        {
            _input = input;
            _config = config;
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.radius = 0.38f;
            _controller.center = new Vector3(0f, 0.9f, 0f);
            _controller.minMoveDistance = 0f;
            _controller.slopeLimit = 45f;

            GameObject visual = GameAssets.AttachCharacter(transform, GameAssets.Player, GameAssets.ZombieAtlas);
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Body";
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                visual.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Create(new Color(0.18f, 0.22f, 0.28f), 0.2f, 0.35f);
                Destroy(visual.GetComponent<Collider>());
            }

            GameAssets.SetLayerRecursively(visual, gameObject.layer);

            var head = new GameObject("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Head = head.transform;
        }

        public void HideBody()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.transform.IsChildOf(Head))
                {
                    continue;
                }

                renderer.enabled = false;
            }
        }

        public void AddRecoil(float pitch)
        {
            _recoilPitch += pitch;
        }

        public void AddHitPunch(float yaw, float pitch)
        {
            Yaw += yaw;
            _recoilPitch += pitch;
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            Vector2 look = _input.Look;
            if (look.sqrMagnitude > 0.0001f)
            {
                Yaw += look.x * _config.PlayerLookSensitivity * Time.deltaTime;
                Pitch -= look.y * _config.PlayerLookSensitivity * Time.deltaTime;
            }

            Vector2 gyro = _input.GyroLook;
            if (gyro.sqrMagnitude > 0.0001f)
            {
                float g = _config.GyroLookSensitivity;
                Yaw += gyro.x * g * Time.deltaTime;
                Pitch += gyro.y * g * Time.deltaTime;
            }
            else if (look.sqrMagnitude <= 0.0001f)
            {
                Vector2 mouse = GameInput.MouseDelta();
                Yaw += mouse.x * _config.EditorLookSensitivity;
                Pitch -= mouse.y * _config.EditorLookSensitivity;
            }
            _recoilPitch = Mathf.MoveTowards(_recoilPitch, 0f, Time.deltaTime * 18f);
            Pitch = Mathf.Clamp(Pitch + _recoilPitch * Time.deltaTime, -55f, 70f);
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);

            Vector3 planar = transform.right * _input.Move.x + transform.forward * _input.Move.y;
            if (planar.sqrMagnitude > 1f)
            {
                planar.Normalize();
            }

            float speed = _config.PlayerMoveSpeed * (_input.SprintHeld ? _config.PlayerSprintMultiplier : 1f);
            if (!_controller.isGrounded)
            {
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
            else if (_verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            Vector3 velocity = planar * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
