#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ShikiShiro.EditorTools
{
    [InitializeOnLoad]
    internal static class OpenMainScene
    {
        static OpenMainScene()
        {
            EditorApplication.delayCall += OpenIfUntitled;
            EditorApplication.delayCall += ForcePlayModeScene;
        }

        private static void ForcePlayModeScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");
            if (scene != null)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
        }

        private static void OpenIfUntitled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                return;
            }

            const string path = "Assets/Scenes/Main.unity";
            if (System.IO.File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        [MenuItem("式城ゾンビアタック/Main シーンを開く")]
        private static void OpenFromMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        }
    }
}
#endif
