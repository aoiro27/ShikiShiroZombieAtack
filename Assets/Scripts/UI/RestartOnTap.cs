using UnityEngine;

namespace ShikiShiro
{
    public sealed class RestartOnTap : MonoBehaviour
    {
        private GameSession _session;
        private float _readyAt;

        public void Bind(GameSession session)
        {
            _session = session;
            session.StateChanged += OnState;
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.StateChanged -= OnState;
            }
        }

        private void OnState(SessionState state)
        {
            if (state == SessionState.GameOver)
            {
                _readyAt = Time.unscaledTime + 0.45f;
            }
        }

        private void Update()
        {
            if (_session == null || _session.State != SessionState.GameOver)
            {
                return;
            }

            if (Time.unscaledTime < _readyAt)
            {
                return;
            }

            if (Pressed())
            {
                Bootstrap.RestartRun();
            }
        }

        private static bool Pressed()
        {
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                TouchPhase phase = Input.GetTouch(i).phase;
                if (phase == TouchPhase.Began || phase == TouchPhase.Ended)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
