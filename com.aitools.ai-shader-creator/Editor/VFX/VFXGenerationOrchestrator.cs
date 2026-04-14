using System;
using System.Collections;

namespace AIShaderCreator.Editor
{
    public class VFXGenerationResult
    {
        public bool Success;
        public string ScriptAssetPath;
        public string ErrorMessage;
    }

    public class VFXGenerationOrchestrator
    {
        private const string BeginMarker = "VFX_CODE_BEGIN";
        private const string EndMarker   = "VFX_CODE_END";

        private readonly IAIClient _client;

        public VFXGenerationOrchestrator(IAIClient client)
        {
            _client = client;
        }

        public IEnumerator GenerateCoroutine(
            string userPrompt,
            ConversationHistory history,
            Action<VFXGenerationResult> onComplete,
            Action<string> onStatus,
            string editPrefabPath = null)
        {
            onStatus?.Invoke("AIにリクエスト中...");

            var systemPrompt = VFXCodeSystemPromptBuilder.Build();
            var messages     = history.ToApiMessages();

            string responseText = null;
            string apiError     = null;

            yield return _client.SendMessageCoroutine(
                systemPrompt, messages, 8192,
                r => responseText = r,
                e => apiError = e
            );

            if (apiError != null)
            {
                onComplete?.Invoke(new VFXGenerationResult { Success = false, ErrorMessage = apiError });
                yield break;
            }

            history.AddAssistantMessage(responseText);

            if (!TryExtractCode(responseText, out var effectCode))
            {
                onComplete?.Invoke(new VFXGenerationResult
                {
                    Success = false,
                    ErrorMessage = "C#コードをレスポンスから抽出できませんでした。"
                });
                yield break;
            }

            onStatus?.Invoke("スクリプトを書き込み中...");
            yield return null;

            string scriptPath;
            try
            {
                VFXCodeWriter.Write(effectCode, out _, editPrefabPath);
                scriptPath = "Assets/GeneratedVFX/_scripts/ (自動削除されます)";
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(new VFXGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"スクリプト書き込みに失敗しました: {ex.Message}"
                });
                yield break;
            }

            onComplete?.Invoke(new VFXGenerationResult
            {
                Success = true,
                ScriptAssetPath = scriptPath
            });
        }

        private static bool TryExtractCode(string response, out string code)
        {
            code = null;
            var begin = response.IndexOf(BeginMarker);
            var end   = response.IndexOf(EndMarker);
            if (begin < 0 || end < 0 || end <= begin) return false;

            code = response.Substring(begin + BeginMarker.Length, end - begin - BeginMarker.Length).Trim();
            // コードフェンスを除去
            if (code.StartsWith("```")) code = code.Substring(code.IndexOf('\n') + 1);
            if (code.EndsWith("```"))   code = code.Substring(0, code.LastIndexOf("```")).TrimEnd();
            return !string.IsNullOrWhiteSpace(code);
        }
    }
}
