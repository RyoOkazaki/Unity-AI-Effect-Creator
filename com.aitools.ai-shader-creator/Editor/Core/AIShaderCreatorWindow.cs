using System.Collections;
using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class AIShaderCreatorWindow : EditorWindow
    {
        // ---- State ----
        private ConversationHistory _history = new();
        private string _inputText = "";
        private bool _isGenerating = false;
        private string _statusMessage = "";
        private bool _applyToSelected = true;
        private Vector2 _chatScrollPos;

        // ---- AI Service selection ----
        private AIService _selectedService = AIService.Claude;
        private static readonly string[] ServiceLabels = { "Claude", "OpenAI", "Gemini" };

        // ---- Services ----
        private ShaderGenerationOrchestrator _orchestrator;

        // ---- Styles ----
        private GUIStyle _userBubble;
        private GUIStyle _assistantBubble;
        private GUIStyle _errorBubble;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized;

        [MenuItem("Tools/AI Shader Creator/Open")]
        public static void Open()
        {
            var w = GetWindow<AIShaderCreatorWindow>("AI Shader Creator");
            w.minSize = new Vector2(420, 520);
        }

        private void OnEnable()
        {
            _selectedService = (AIService)EditorPrefs.GetInt("AIShaderCreator_Service", 0);
            RebuildOrchestrator();
        }

        private void RebuildOrchestrator()
        {
            if (!AIServiceFactory.HasKey(_selectedService)) return;
            var model = EditorPrefs.GetString(
                AIServiceFactory.GetModelPrefsKey(_selectedService),
                AIServiceFactory.GetDefaultModel(_selectedService));
            var client = AIServiceFactory.Create(_selectedService, model);
            _orchestrator = new ShaderGenerationOrchestrator(client);
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _userBubble = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                fontSize = 12,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
            _assistantBubble = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 1f, 0.85f) }
            };
            _errorBubble = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                fontSize = 12,
                normal = { textColor = new Color(1f, 0.6f, 0.6f) }
            };
            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
        }

        private void OnGUI()
        {
            InitStyles();

            // ---- APIキー未設定の警告 ----
            if (!AIServiceFactory.HasKey(_selectedService))
            {
                EditorGUILayout.HelpBox(
                    $"{_selectedService} の APIキーが設定されていません。\nTools > AI Shader Creator > Settings から設定してください。",
                    MessageType.Warning);
                if (GUILayout.Button("Settings を開く"))
                    SettingsWindow.Open();
            }

            // ---- サービス選択 ----
            DrawServiceSelector();

            // ---- チャット履歴 ----
            DrawChatHistory();

            // ---- ステータス ----
            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.LabelField(_statusMessage, _statusStyle);

            EditorGUILayout.Space(4);

            // ---- 対象GameObject ----
            _applyToSelected = EditorGUILayout.ToggleLeft(
                "選択中のGameObjectにマテリアルを適用", _applyToSelected);

            // ---- 入力エリア ----
            DrawInputArea();
        }

        private void DrawServiceSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("AIサービス:", GUILayout.Width(72));

            var newService = (AIService)EditorGUILayout.Popup((int)_selectedService, ServiceLabels);
            if (newService != _selectedService)
            {
                _selectedService = newService;
                EditorPrefs.SetInt("AIShaderCreator_Service", (int)_selectedService);
                RebuildOrchestrator();
            }

            var hasKey = AIServiceFactory.HasKey(_selectedService);
            var keyColor = hasKey ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            var prevColor = GUI.color;
            GUI.color = keyColor;
            GUILayout.Label(hasKey ? "● キー設定済" : "● キー未設定", GUILayout.Width(100));
            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private void DrawChatHistory()
        {
            var chatHeight = position.height - 180;
            _chatScrollPos = EditorGUILayout.BeginScrollView(
                _chatScrollPos, GUILayout.Height(chatHeight));

            foreach (var msg in _history.Messages)
            {
                switch (msg.Role)
                {
                    case MessageRole.User:
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"🙋 {msg.Content}", _userBubble,
                            GUILayout.MaxWidth(position.width * 0.75f));
                        EditorGUILayout.EndHorizontal();
                        break;

                    case MessageRole.Assistant:
                        var displayContent = TruncateShaderCode(msg.Content);
                        EditorGUILayout.LabelField($"🤖 {displayContent}", _assistantBubble);
                        break;

                    case MessageRole.System when msg.IsError:
                        EditorGUILayout.LabelField($"⚠️ {msg.Content}", _errorBubble);
                        break;

                    case MessageRole.System:
                        EditorGUILayout.LabelField($"ℹ️ {msg.Content}", _assistantBubble);
                        break;
                }
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawInputArea()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_isGenerating;
            _inputText = EditorGUILayout.TextArea(_inputText,
                GUILayout.Height(50), GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            if (GUILayout.Button(_isGenerating ? "生成中..." : "送信",
                GUILayout.Height(50)) && !_isGenerating)
            {
                SendMessage();
            }

            if (GUILayout.Button("クリア", GUILayout.Height(24)))
            {
                _history.Clear();
                _inputText = "";
                _statusMessage = "";
                Repaint();
            }
            EditorGUILayout.EndVertical();
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return
                && e.shift && !_isGenerating && !string.IsNullOrWhiteSpace(_inputText))
            {
                SendMessage();
                e.Use();
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_inputText)) return;

            RebuildOrchestrator();
            if (_orchestrator == null)
            {
                _history.AddErrorMessage($"{_selectedService} の APIキーが設定されていません。");
                Repaint();
                return;
            }

            var userInput = _inputText.Trim();
            _inputText = "";
            _isGenerating = true;
            _history.AddUserMessage(userInput);
            _chatScrollPos.y = float.MaxValue;
            Repaint();

            EditorCoroutineRunner.Run(GenerateCoroutine(userInput));
        }

        private IEnumerator GenerateCoroutine(string userInput)
        {
            GenerationResult result = null;

            yield return _orchestrator.GenerateCoroutine(
                userInput,
                _history,
                _applyToSelected,
                r => result = r,
                status =>
                {
                    _statusMessage = status;
                    Repaint();
                }
            );

            _isGenerating = false;
            _statusMessage = "";

            if (result != null)
            {
                if (result.Success)
                {
                    var msg = $"✅ シェーダー '{result.ShaderName}' を生成しました。\n" +
                              $"パス: {result.ShaderAssetPath}";
                    if (result.WasAutoFixed) msg += "\n（コンパイルエラーを自動修正しました）";
                    if (_applyToSelected && Selection.activeGameObject != null)
                        msg += $"\n📎 {Selection.activeGameObject.name} に適用しました";
                    _history.AddAssistantMessage(msg);
                }
                else
                {
                    _history.AddErrorMessage(result.ErrorMessage ?? "不明なエラーが発生しました。");
                    if (!string.IsNullOrEmpty(result.ShaderAssetPath))
                    {
                        var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(result.ShaderAssetPath);
                        if (shader != null) EditorGUIUtility.PingObject(shader);
                    }
                }
            }

            _chatScrollPos.y = float.MaxValue;
            Repaint();
        }

        private string TruncateShaderCode(string content)
        {
            const int MaxLen = 300;
            if (content.Length <= MaxLen) return content;

            var beginIdx = content.IndexOf("SHADER_BEGIN");
            if (beginIdx < 0) return content.Substring(0, MaxLen) + "...";

            var before = content.Substring(0, beginIdx).Trim();
            return string.IsNullOrEmpty(before)
                ? "[シェーダーコードを生成しました]"
                : $"{before}\n[シェーダーコードを生成しました]";
        }
    }
}
