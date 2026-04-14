using System.Collections;
using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class AIShaderCreatorWindow : EditorWindow
    {
        // ---- Mode ----
        private enum EditorMode { Shader, VFX }
        private EditorMode _mode = EditorMode.Shader;
        private static readonly string[] ModeLabels = { "Shader", "VFX Effect" };

        // ---- State ----
        private ConversationHistory _shaderHistory = new();
        private ConversationHistory _vfxHistory = new();
        private string _inputText = "";
        private bool _isGenerating = false;
        private string _statusMessage = "";
        private bool _applyToSelected = true;
        private Vector2 _chatScrollPos;

        // ---- 編集追跡 ----
        private string _lastShaderPath = null;
        private string _lastVFXPrefabPath = null;

        // ---- AI Service selection ----
        private AIService _selectedService = AIService.Claude;
        private static readonly string[] ServiceLabels = { "Claude", "OpenAI", "Gemini" };

        // ---- Orchestrators ----
        private ShaderGenerationOrchestrator _shaderOrchestrator;
        private VFXGenerationOrchestrator _vfxOrchestrator;

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
            RebuildOrchestrators();
        }

        private void RebuildOrchestrators()
        {
            if (!AIServiceFactory.HasKey(_selectedService)) return;
            var model = EditorPrefs.GetString(
                AIServiceFactory.GetModelPrefsKey(_selectedService),
                AIServiceFactory.GetDefaultModel(_selectedService));
            var client = AIServiceFactory.Create(_selectedService, model);
            _shaderOrchestrator = new ShaderGenerationOrchestrator(client);
            _vfxOrchestrator = new VFXGenerationOrchestrator(client);
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

            // ---- モード切替 ----
            DrawModeSelector();

            // ---- チャット履歴 ----
            DrawChatHistory();

            // ---- ステータス ----
            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.LabelField(_statusMessage, _statusStyle);

            EditorGUILayout.Space(4);

            // ---- オプション ----
            if (_mode == EditorMode.Shader)
            {
                _applyToSelected = EditorGUILayout.ToggleLeft(
                    "選択中のGameObjectにマテリアルを適用", _applyToSelected);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "エフェクトは Assets/GeneratedVFX/ にプレハブとして保存されます。",
                    MessageType.Info);
            }

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
                RebuildOrchestrators();
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

        private void DrawModeSelector()
        {
            var newMode = (EditorMode)GUILayout.Toolbar((int)_mode, ModeLabels);
            if (newMode != _mode)
            {
                _mode = newMode;
                _statusMessage = "";
                _chatScrollPos.y = float.MaxValue;
                Repaint();
            }

            // 編集モード表示 + 新規作成ボタン
            var editPath = _mode == EditorMode.Shader ? _lastShaderPath : _lastVFXPrefabPath;
            if (editPath != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"編集中: {System.IO.Path.GetFileName(editPath)}", MessageType.None);
                if (GUILayout.Button("新規作成", GUILayout.Width(70), GUILayout.Height(30)))
                {
                    if (_mode == EditorMode.Shader) { _lastShaderPath = null; _shaderHistory.Clear(); }
                    else { _lastVFXPrefabPath = null; _vfxHistory.Clear(); }
                    _statusMessage = "";
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(4);
        }

        private ConversationHistory CurrentHistory =>
            _mode == EditorMode.Shader ? _shaderHistory : _vfxHistory;

        private void DrawChatHistory()
        {
            var chatHeight = position.height - (_mode == EditorMode.Shader ? 195 : 210);
            _chatScrollPos = EditorGUILayout.BeginScrollView(
                _chatScrollPos, GUILayout.Height(chatHeight));

            foreach (var msg in CurrentHistory.Messages)
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
                        var displayContent = _mode == EditorMode.Shader
                            ? TruncateShaderCode(msg.Content)
                            : TruncateVFXJson(msg.Content);
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
            var placeholder = _mode == EditorMode.Shader
                ? "シェーダーの説明を入力... (Shift+Enter で送信)"
                : "エフェクトの説明を入力... 例: 青白い炎のような魔法エフェクト (Shift+Enter で送信)";

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
                CurrentHistory.Clear();
                _inputText = "";
                _statusMessage = "";
                if (_mode == EditorMode.Shader) _lastShaderPath = null;
                else _lastVFXPrefabPath = null;
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

            RebuildOrchestrators();
            if (_shaderOrchestrator == null || _vfxOrchestrator == null)
            {
                CurrentHistory.AddErrorMessage($"{_selectedService} の APIキーが設定されていません。");
                Repaint();
                return;
            }

            var userInput = _inputText.Trim();
            _inputText = "";
            _isGenerating = true;
            CurrentHistory.AddUserMessage(userInput);
            _chatScrollPos.y = float.MaxValue;
            Repaint();

            if (_mode == EditorMode.Shader)
                EditorCoroutineRunner.Run(GenerateShaderCoroutine(userInput));
            else
                EditorCoroutineRunner.Run(GenerateVFXCoroutine(userInput));
        }

        private IEnumerator GenerateShaderCoroutine(string userInput)
        {
            GenerationResult result = null;

            yield return _shaderOrchestrator.GenerateCoroutine(
                userInput, _shaderHistory, _applyToSelected,
                r => result = r,
                status => { _statusMessage = status; Repaint(); },
                editPath: _lastShaderPath
            );

            _isGenerating = false;
            _statusMessage = "";

            if (result != null)
            {
                if (result.Success)
                {
                    _lastShaderPath = result.ShaderAssetPath;
                    var action = _lastShaderPath != null && result.ShaderAssetPath == _lastShaderPath ? "更新" : "生成";
                    var msg = $"✅ シェーダー '{result.ShaderName}' を{action}しました。\nパス: {result.ShaderAssetPath}";
                    if (result.WasAutoFixed) msg += "\n（コンパイルエラーを自動修正しました）";
                    if (_applyToSelected && Selection.activeGameObject != null)
                        msg += $"\n📎 {Selection.activeGameObject.name} に適用しました";
                    _shaderHistory.AddAssistantMessage(msg);
                }
                else
                {
                    _shaderHistory.AddErrorMessage(result.ErrorMessage ?? "不明なエラーが発生しました。");
                    if (!string.IsNullOrEmpty(result.ShaderAssetPath))
                    {
                        var shader = AssetDatabase.LoadAssetAtPath<Shader>(result.ShaderAssetPath);
                        if (shader != null) EditorGUIUtility.PingObject(shader);
                    }
                }
            }

            _chatScrollPos.y = float.MaxValue;
            Repaint();
        }

        private IEnumerator GenerateVFXCoroutine(string userInput)
        {
            VFXGenerationResult result = null;

            yield return _vfxOrchestrator.GenerateCoroutine(
                userInput, _vfxHistory,
                r => result = r,
                status => { _statusMessage = status; Repaint(); },
                editPrefabPath: _lastVFXPrefabPath
            );

            _isGenerating = false;
            _statusMessage = "";

            if (result != null)
            {
                if (result.Success)
                {
                    var action = _lastVFXPrefabPath != null ? "更新" : "生成";
                    var msg = $"✅ エフェクトコードを{action}しました。\n\nUnity が自動コンパイル後、Assets/GeneratedVFX/ にプレハブが作成されます。\nコンパイルが完了したら Project ウィンドウを確認してください。";
                    _vfxHistory.AddAssistantMessage(msg);
                    // コンパイル後にプレハブパスが確定するので SessionState から取得
                    _lastVFXPrefabPath = VFXAutoFixer.GetLastPrefabPath();
                }
                else
                {
                    _vfxHistory.AddErrorMessage(result.ErrorMessage ?? "不明なエラーが発生しました。");
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

        private string TruncateVFXJson(string content)
        {
            const int MaxLen = 300;
            if (content.Length <= MaxLen) return content;
            var beginIdx = content.IndexOf("VFX_BEGIN");
            if (beginIdx < 0) return content.Substring(0, MaxLen) + "...";
            var before = content.Substring(0, beginIdx).Trim();
            return string.IsNullOrEmpty(before)
                ? "[エフェクト設定を生成しました]"
                : $"{before}\n[エフェクト設定を生成しました]";
        }
    }
}
