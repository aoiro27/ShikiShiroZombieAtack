using UnityEngine;
using UnityEngine.EventSystems;
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

            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(0);
                    return;
                }

                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }
}
