using UnityEngine;

namespace ShikiShiro
{
    public sealed class GameInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public Vector2 GyroLook { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool SwapPressed { get; private set; }
        public bool PausePressed { get; private set; }

        private VirtualJoystick _moveStick;
        private VirtualJoystick _lookStick;
        private Quaternion _prevGyroAtt;
        private bool _gyroReady;
        private ScreenOrientation _gyroOrientation;
        private bool _touchFire;
        private bool _touchReload;
        private bool _touchSprint;
        private bool _touchSwap;
        private bool _touchPause;

        public void Bind(VirtualJoystick move, VirtualJoystick look)
        {
            _moveStick = move;
            _lookStick = look;
            EnableGyro();
        }

        private void Awake()
        {
            EnableGyro();
        }

        private static void EnableGyro()
        {
            if (!SystemInfo.supportsGyroscope)
            {
                return;
            }

            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 0.0111f;
        }

        public void SetTouchFire(bool held) => _touchFire = held;
        public void PulseReload() => _touchReload = true;
        public void SetTouchSprint(bool held) => _touchSprint = held;
        public void PulseSwap() => _touchSwap = true;
        public void PulsePause() => _touchPause = true;

        private void Update()
        {
            Vector2 move = _moveStick != null ? _moveStick.Value : Vector2.zero;
            move += new Vector2(KeyboardAxis(KeyCode.A, KeyCode.D), KeyboardAxis(KeyCode.S, KeyCode.W));
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = _lookStick != null ? _lookStick.Value : Vector2.zero;
            GyroLook = ReadGyro();
            FireHeld = _touchFire || Input.GetKey(KeyCode.Space);
            ReloadPressed = _touchReload || Input.GetKeyDown(KeyCode.R);
            SprintHeld = _touchSprint || Input.GetKey(KeyCode.LeftShift);
            SwapPressed = _touchSwap || Input.GetKeyDown(KeyCode.Q);
            PausePressed = _touchPause || Input.GetKeyDown(KeyCode.Escape);

            _touchReload = false;
            _touchSwap = false;
            _touchPause = false;
        }

        private static float KeyboardAxis(KeyCode neg, KeyCode pos)
        {
            float v = 0f;
            if (Input.GetKey(neg))
            {
                v -= 1f;
            }

            if (Input.GetKey(pos))
            {
                v += 1f;
            }

            return v;
        }

        private Vector2 ReadGyro()
        {
            if (!SystemInfo.supportsGyroscope || !Input.gyro.enabled)
            {
                _gyroReady = false;
                return Vector2.zero;
            }

            Quaternion att = ScreenMappedGyro(Input.gyro.attitude);
            ScreenOrientation orient = Screen.orientation;
            if (!_gyroReady || _gyroOrientation != orient)
            {
                _prevGyroAtt = att;
                _gyroOrientation = orient;
                _gyroReady = true;
                return Vector2.zero;
            }

            Vector3 prevFwd = _prevGyroAtt * Vector3.forward;
            Vector3 nowFwd = att * Vector3.forward;
            _prevGyroAtt = att;

            Vector3 prevFlat = prevFwd;
            Vector3 nowFlat = nowFwd;
            prevFlat.y = 0f;
            nowFlat.y = 0f;
            float yaw = 0f;
            if (prevFlat.sqrMagnitude > 0.0025f && nowFlat.sqrMagnitude > 0.0025f)
            {
                yaw = Vector3.SignedAngle(prevFlat, nowFlat, Vector3.up);
            }

            float prevPitch = Mathf.Asin(Mathf.Clamp(prevFwd.y, -1f, 1f)) * Mathf.Rad2Deg;
            float nowPitch = Mathf.Asin(Mathf.Clamp(nowFwd.y, -1f, 1f)) * Mathf.Rad2Deg;
            float pitch = Mathf.DeltaAngle(prevPitch, nowPitch);
            if (Mathf.Abs(yaw) > 35f || Mathf.Abs(pitch) > 35f)
            {
                return Vector2.zero;
            }

            if (Mathf.Abs(yaw) < 0.04f && Mathf.Abs(pitch) < 0.04f)
            {
                return Vector2.zero;
            }

            const float maxStep = 24f;
            return new Vector2(Mathf.Clamp(yaw, -maxStep, maxStep), Mathf.Clamp(pitch, -maxStep, maxStep));
        }

        private static Quaternion ScreenMappedGyro(Quaternion attitude)
        {
            Quaternion q = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
            return Quaternion.Euler(90f, 0f, 0f) * q * Quaternion.Euler(0f, 0f, 90f);
        }

        public static Vector2 MouseDelta()
        {
            return Input.mousePositionDelta;
        }
    }
}
