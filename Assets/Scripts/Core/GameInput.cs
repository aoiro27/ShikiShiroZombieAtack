using UnityEngine;

namespace ShikiShiro
{
    public sealed class GameInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool FireHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool SwapPressed { get; private set; }
        public bool PausePressed { get; private set; }

        private VirtualJoystick _moveStick;
        private VirtualJoystick _lookStick;
        private bool _touchFire;
        private bool _touchReload;
        private bool _touchSprint;
        private bool _touchSwap;
        private bool _touchPause;

        public void Bind(VirtualJoystick move, VirtualJoystick look)
        {
            _moveStick = move;
            _lookStick = look;
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
            FireHeld = _touchFire || Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Space);
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

        public static Vector2 MouseDelta()
        {
            return Input.mousePositionDelta;
        }
    }
}
