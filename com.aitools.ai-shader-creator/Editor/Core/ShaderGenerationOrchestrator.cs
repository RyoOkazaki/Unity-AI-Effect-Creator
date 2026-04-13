using System;
using System.Collections;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class GenerationResult
    {
        public bool Success;
        public string ShaderAssetPath;
        public string MaterialAssetPath;
        public string ShaderName;
        public string ErrorMessage;
        public bool WasAutoFixed;
    }

    public class ShaderGenerationOrchestrator
    {
        private readonly IAIClient _client;
        private readonly ShaderAutoFixer _fixer;

        public ShaderGenerationOrchestrator(IAIClient client)
        {
            _client = client;
            _fixer = new ShaderAutoFixer(client);
        }

        public IEnumerator GenerateCoroutine(
            string userPrompt,
            ConversationHistory history,
            bool applyToSelected,
            Action<GenerationResult> onComplete,
            Action<string> onStatus)
        {
            onStatus?.Invoke("AIにリクエスト中...");

            var hasExistingShader = !string.IsNullOrEmpty(GetLastShaderCode(history));
            var systemPrompt = hasExistingShader
                ? SystemPromptBuilder.BuildContinuationPrompt()
                : SystemPromptBuilder.BuildShaderGenerationPrompt();

            var messages = history.ToApiMessages();

            string apiResponseText = null;
            string apiError = null;

            yield return _client.SendMessageCoroutine(
                systemPrompt, messages, 8192,
                r => apiResponseText = r,
                e => apiError = e
            );

            if (apiError != null)
            {
                onComplete?.Invoke(new GenerationResult { Success = false, ErrorMessage = apiError });
                yield break;
            }

            history.AddAssistantMessage(apiResponseText);

            if (!ShaderParser.TryExtractShaderCode(apiResponseText, out var shaderCode))
            {
                onComplete?.Invoke(new GenerationResult
                {
                    Success = false,
                    ErrorMessage = "シェーダーコードをレスポンスから抽出できませんでした。"
                });
                yield break;
            }

            var shaderName = ShaderParser.ExtractShaderName(shaderCode);
            onStatus?.Invoke($"シェーダーを保存中: {shaderName}");

            var assetPath = ShaderFileWriter.Write(shaderCode, shaderName);

            yield return null;
            yield return null;

            var errors = ShaderValidator.GetErrors(assetPath);
            var wasFixed = false;

            if (errors.Length > 0)
            {
                onStatus?.Invoke($"コンパイルエラー検出 ({errors.Length}件)。自動修正中...");

                for (int attempt = 1; attempt <= ShaderAutoFixer.MaxAttempts; attempt++)
                {
                    onStatus?.Invoke($"自動修正中... (試行 {attempt}/{ShaderAutoFixer.MaxAttempts})");

                    bool fixSuccess = false;
                    string fixError = null;

                    yield return _fixer.AttemptFixCoroutine(
                        assetPath, errors, attempt,
                        _ => { fixSuccess = true; wasFixed = true; },
                        e => fixError = e
                    );

                    if (fixSuccess)
                    {
                        errors = ShaderValidator.GetErrors(assetPath);
                        if (errors.Length == 0) break;
                    }

                    if (attempt == ShaderAutoFixer.MaxAttempts)
                    {
                        onComplete?.Invoke(new GenerationResult
                        {
                            Success = false,
                            ShaderAssetPath = assetPath,
                            ShaderName = shaderName,
                            ErrorMessage = $"自動修正に失敗しました ({ShaderAutoFixer.MaxAttempts}回試行)。\n" +
                                           $"エラー: {string.Join("\n", System.Array.ConvertAll(errors, e => e.ToString()))}"
                        });
                        yield break;
                    }
                }
            }

            onStatus?.Invoke("マテリアルを作成中...");
            var mat = MaterialApplicator.CreateAndSave(assetPath, shaderName);

            if (applyToSelected && mat != null)
                MaterialApplicator.ApplyToSelectedObject(mat);

            onComplete?.Invoke(new GenerationResult
            {
                Success = true,
                ShaderAssetPath = assetPath,
                ShaderName = shaderName,
                WasAutoFixed = wasFixed
            });
        }

        private string GetLastShaderCode(ConversationHistory history)
        {
            var messages = history.Messages;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Role == MessageRole.Assistant &&
                    messages[i].Content.Contains("SHADER_BEGIN"))
                    return messages[i].Content;
            }
            return null;
        }
    }
}
