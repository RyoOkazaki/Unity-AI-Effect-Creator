using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class SettingsWindow : EditorWindow
    {
        private string _apiKeyInput = "";
        private string _selectedModel = ClaudeApiClient.ModelOpus;
        private bool _showKey = false;
        private string _statusMessage = "";

        [MenuItem("Tools/AI Shader Creator/Settings")]
        public static void Open()
        {
            var w = GetWindow<SettingsWindow>("AI Shader Creator - Settings");
            w.minSize = new Vector2(420, 220);
            w.LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            _apiKeyInput = ApiKeyStorage.Load();
            _selectedModel = EditorPrefs.GetString("AIShaderCreator_Model", ClaudeApiClient.ModelOpus);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Claude API 設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Claude APIキーを入力してください。\n" +
                "APIキーは https://console.anthropic.com/ で取得できます。",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // APIキー入力
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("API Key:", GUILayout.Width(70));
            _apiKeyInput = _showKey
                ? EditorGUILayout.TextField(_apiKeyInput)
                : EditorGUILayout.PasswordField(_apiKeyInput);
            _showKey = GUILayout.Toggle(_showKey, "表示", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // モデル選択
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("モデル:", GUILayout.Width(70));
            var models = new[] { ClaudeApiClient.ModelOpus, ClaudeApiClient.ModelHaiku };
            var labels = new[] { "Opus 4.6 (高品質)", "Haiku 4.5 (高速・低コスト)" };
            var idx = System.Array.IndexOf(models, _selectedModel);
            idx = EditorGUILayout.Popup(idx < 0 ? 0 : idx, labels);
            _selectedModel = models[idx];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存", GUILayout.Height(30)))
            {
                ApiKeyStorage.Save(_apiKeyInput);
                EditorPrefs.SetString("AIShaderCreator_Model", _selectedModel);
                _statusMessage = "✓ 保存しました";
                EditorApplication.delayCall += () => { _statusMessage = ""; Repaint(); };
            }
            if (GUILayout.Button("クリア", GUILayout.Height(30), GUILayout.Width(80)))
            {
                ApiKeyStorage.Clear();
                _apiKeyInput = "";
                _statusMessage = "APIキーを削除しました";
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }
    }
}
