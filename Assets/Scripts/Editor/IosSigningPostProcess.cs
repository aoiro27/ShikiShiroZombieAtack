#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace ShikiShiro.EditorTools
{
    public sealed class IosSigningWindow : EditorWindow
    {
        private const string PrefsKey = "ShikiShiro.IosTeamId";
        private string _teamId;

        [MenuItem("式城ゾンビアタック/iOS 署名 (Team ID)")]
        public static void Open()
        {
            var window = GetWindow<IosSigningWindow>(true, "iOS Team ID");
            window.minSize = new Vector2(420f, 140f);
            window.Show();
        }

        private void OnEnable()
        {
            _teamId = EditorPrefs.GetString(PrefsKey, PlayerSettings.iOS.appleDeveloperTeamID ?? string.Empty);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Xcode の Signing & Capabilities に出る 10 文字の Team ID を一度だけ保存します。");
            EditorGUILayout.LabelField("Unity が Xcode プロジェクトを作り直しても、自動署名で同じチームが入ります。");
            EditorGUILayout.Space(8f);
            _teamId = EditorGUILayout.TextField("Team ID", _teamId);
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("保存"))
            {
                string team = (_teamId ?? string.Empty).Trim().ToUpperInvariant();
                if (team.Length > 0 && team.Length != 10)
                {
                    EditorUtility.DisplayDialog("iOS Team ID", "Team ID は 10 文字です。Xcode の Signing からコピーしてください。", "OK");
                    return;
                }

                EditorPrefs.SetString(PrefsKey, team);
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                PlayerSettings.iOS.appleDeveloperTeamID = team;
                Close();
            }
        }

        [PostProcessBuild(999)]
        public static void ApplySigning(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string team = EditorPrefs.GetString(PrefsKey, string.Empty).Trim();
            if (string.IsNullOrEmpty(team))
            {
                team = PlayerSettings.iOS.appleDeveloperTeamID;
            }

#if UNITY_IOS
            string pbxPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(pbxPath);
            Stamp(proj, proj.GetUnityMainTargetGuid(), team);
            Stamp(proj, proj.GetUnityFrameworkTargetGuid(), team);
            proj.WriteToFile(pbxPath);

            string plistPath = path + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("CFBundleDisplayName", "しきしろぞんびーず");
            plist.root.SetString("CFBundleName", "しきしろぞんびーず");
            plist.WriteToFile(plistPath);
#endif
        }

#if UNITY_IOS
        private static void Stamp(PBXProject proj, string guid, string team)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            proj.SetBuildProperty(guid, "CODE_SIGN_STYLE", "Automatic");
            if (!string.IsNullOrEmpty(team))
            {
                proj.SetBuildProperty(guid, "DEVELOPMENT_TEAM", team);
            }
        }
#endif
    }
}
#endif
