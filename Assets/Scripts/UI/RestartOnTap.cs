using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShikiShiro
{
    public sealed class RestartOnTap : MonoBehaviour
    {
        private GameSession _session;

        public void Bind(GameSession session)
        {
            _session = session;
        }

        private void Update()
        {
            if (_session == null || _session.State != SessionState.GameOver)
            {
                return;
            }

            bool pressed = Input.GetMouseButtonDown(0);
            if (!pressed && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pressed = true;
            }

            if (pressed)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }
}
