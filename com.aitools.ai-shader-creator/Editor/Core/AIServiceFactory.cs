using System;

namespace AIShaderCreator.Editor
{
    public enum AIService { Claude, OpenAI, Gemini }

    public static class AIServiceFactory
    {
        public static IAIClient Create(AIService service, string model = null)
        {
            var apiKey = ApiKeyStorage.Load(service);
            return service switch
            {
                AIService.Claude => new ClaudeApiClient(apiKey, model ?? ClaudeApiClient.ModelOpus),
                AIService.OpenAI => new OpenAIClient(apiKey, model ?? OpenAIClient.ModelGpt4o),
                AIService.Gemini => new GeminiClient(apiKey, model ?? GeminiClient.ModelFlash),
                _ => throw new ArgumentException($"Unknown service: {service}")
            };
        }

        public static bool HasKey(AIService service) => ApiKeyStorage.HasKey(service);

        public static string[] GetModels(AIService service) => service switch
        {
            AIService.Claude => new[] { ClaudeApiClient.ModelOpus, ClaudeApiClient.ModelHaiku },
            AIService.OpenAI => new[] { OpenAIClient.ModelGpt4o, OpenAIClient.ModelGpt4oMini },
            AIService.Gemini => new[] { GeminiClient.ModelFlash, GeminiClient.ModelPro },
            _ => Array.Empty<string>()
        };

        public static string[] GetModelLabels(AIService service) => service switch
        {
            AIService.Claude => new[] { "Opus 4.6 (高品質)", "Haiku 4.5 (高速・低コスト)" },
            AIService.OpenAI => new[] { "GPT-4o (高品質)", "GPT-4o mini (高速・低コスト)" },
            AIService.Gemini => new[] { "Gemini 2.0 Flash (高速)", "Gemini 1.5 Pro (高品質)" },
            _ => Array.Empty<string>()
        };

        public static string GetModelPrefsKey(AIService service)
            => $"AIShaderCreator_Model_{service}";

        public static string GetDefaultModel(AIService service) => service switch
        {
            AIService.Claude => ClaudeApiClient.ModelOpus,
            AIService.OpenAI => OpenAIClient.ModelGpt4o,
            AIService.Gemini => GeminiClient.ModelFlash,
            _ => ""
        };
    }
}
