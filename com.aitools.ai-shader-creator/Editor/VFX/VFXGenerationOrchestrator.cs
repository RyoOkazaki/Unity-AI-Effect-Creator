using System;
using System.Collections;

namespace AIShaderCreator.Editor
{
    public class VFXGenerationResult
    {
        public bool Success;
        public string PrefabAssetPath;
        public string EffectName;
        public string ErrorMessage;
    }

    public class VFXGenerationOrchestrator
    {
        private readonly IAIClient _client;

        public VFXGenerationOrchestrator(IAIClient client)
        {
            _client = client;
        }

        public IEnumerator GenerateCoroutine(
            string userPrompt,
            ConversationHistory history,
            Action<VFXGenerationResult> onComplete,
            Action<string> onStatus)
        {
            onStatus?.Invoke("AIにリクエスト中...");

            var systemPrompt = VFXSystemPromptBuilder.BuildVFXGenerationPrompt();
            var messages = history.ToApiMessages();

            string responseText = null;
            string apiError = null;

            yield return _client.SendMessageCoroutine(
                systemPrompt, messages, 4096,
                r => responseText = r,
                e => apiError = e
            );

            if (apiError != null)
            {
                onComplete?.Invoke(new VFXGenerationResult { Success = false, ErrorMessage = apiError });
                yield break;
            }

            history.AddAssistantMessage(responseText);

            if (!VFXParser.TryExtractConfig(responseText, out var config))
            {
                onComplete?.Invoke(new VFXGenerationResult
                {
                    Success = false,
                    ErrorMessage = "エフェクト設定をレスポンスから抽出できませんでした。"
                });
                yield break;
            }

            onStatus?.Invoke($"エフェクトを生成中: {config.displayName}...");
            yield return null;

            string prefabPath;
            try
            {
                prefabPath = ParticleEffectFactory.CreatePrefab(config);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(new VFXGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"プレハブ生成に失敗しました: {ex.Message}"
                });
                yield break;
            }

            onComplete?.Invoke(new VFXGenerationResult
            {
                Success = true,
                PrefabAssetPath = prefabPath,
                EffectName = config.displayName ?? config.effectType
            });
        }
    }
}
