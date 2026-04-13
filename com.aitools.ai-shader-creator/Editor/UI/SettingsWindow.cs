using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class SettingsWindow : EditorWindow
    {
        private AIService _selectedTab = AIService.Claude;

        // Per-service state
        private readonly string[] _apiKeyInputs = new string[3];
        private readonly string[] _selectedModels = new string[3];
        private readonly bool[] _showKeys = new bool[3];
        private string _statusMessage = "";

        [MenuItem("Tools/AI Shader Creator/Settings")]
        public static void Open()
        {
            var w = GetWindow<SettingsWindow>("AI Shader Creator - Settings");
            w.minSize = new Vector2(460, 260);
            w.LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            foreach (AIService s in System.Enum.GetValues(typeof(AIService)))
            {
                var i = (int)s;
                _apiKeyInputs[i] = ApiKeyStorage.Load(s);
                _selectedModels[i] = EditorPrefs.GetString(
                    AIServiceFactory.GetModelPrefsKey(s),
                    AIServiceFactory.GetDefaultModel(s));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("AI サービス設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Tab selector
            EditorGUILayout.BeginHorizontal();
            foreach (AIService s in System.Enum.GetValues(typeof(AIService)))
            {
                var label = s switch
                {
                    AIService.Claude => "Claude",
                    AIService.OpenAI => "OpenAI",
                    AIService.Gemini => "Gemini",
                    _ => s.ToString()
                };
                var style = _selectedTab == s ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
                var prevColor = GUI.backgroundColor;
                if (_selectedTab == s) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button(label, style)) _selectedTab = s;
                GUI.backgroundColor = prevColor;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            DrawServicePanel(_selectedTab);

            EditorGUILayout.Space(8);

            // Save / Clear buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存", GUILayout.Height(30)))
            {
                SaveAll();
                _statusMessage = "✓ 保存しました";
                EditorApplication.delayCall += () => { _statusMessage = ""; Repaint(); };
            }
            if (GUILayout.Button($"{_selectedTab} キーをクリア", GUILayout.Height(30), GUILayout.Width(180)))
            {
                ApiKeyStorage.Clear(_selectedTab);
                _apiKeyInputs[(int)_selectedTab] = "";
                _statusMessage = $"{_selectedTab} の APIキーを削除しました";
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }

        private void DrawServicePanel(AIService service)
        {
            var i = (int)service;
            var (keyUrl, keyLabel) = service switch
            {
                AIService.Claude => ("https://console.anthropic.com/", "Claude API キー"),
                AIService.OpenAI => ("https://platform.openai.com/api-keys", "OpenAI API キー"),
                AIService.Gemini => ("https://aistudio.google.com/app/apikey", "Gemini API キー"),
                _ => ("", "API キー")
            };

            EditorGUILayout.HelpBox(
                $"{keyLabel}を入力してください。\n取得URL: {keyUrl}",
                MessageType.Info);

            EditorGUILayout.Space(6);

            // API key input
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("API Key:", GUILayout.Width(70));
            _apiKeyInputs[i] = _showKeys[i]
                ? EditorGUILayout.TextField(_apiKeyInputs[i])
                : EditorGUILayout.PasswordField(_apiKeyInputs[i]);
            _showKeys[i] = GUILayout.Toggle(_showKeys[i], "表示", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Model selector
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("モデル:", GUILayout.Width(70));
            var models = AIServiceFactory.GetModels(service);
            var labels = AIServiceFactory.GetModelLabels(service);
            var idx = System.Array.IndexOf(models, _selectedModels[i]);
            idx = EditorGUILayout.Popup(idx < 0 ? 0 : idx, labels);
            _selectedModels[i] = models[idx];
            EditorGUILayout.EndHorizontal();
        }

        private void SaveAll()
        {
            foreach (AIService s in System.Enum.GetValues(typeof(AIService)))
            {
                var i = (int)s;
                ApiKeyStorage.Save(s, _apiKeyInputs[i]);
                EditorPrefs.SetString(AIServiceFactory.GetModelPrefsKey(s), _selectedModels[i]);
            }
        }
    }
}
