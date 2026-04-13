using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class ShaderAutoFixer
    {
        public const int MaxAttempts = 3;

        private readonly IAIClient _client;

        public ShaderAutoFixer(IAIClient client)
        {
            _client = client;
        }

        public IEnumerator AttemptFixCoroutine(
            string shaderAssetPath,
            ShaderError[] errors,
            int attemptNumber,
            Action<string> onFixed,
            Action<string> onFailed)
        {
            var absolutePath = Path.Combine(
                Application.dataPath.Replace("Assets", ""), shaderAssetPath);

            if (!File.Exists(absolutePath))
            {
                onFailed?.Invoke("シェーダーファイルが見つかりません。");
                yield break;
            }

            var originalCode = File.ReadAllText(absolutePath);
            var errorsJson = ShaderValidator.ToJson(errors);
            var systemPrompt = SystemPromptBuilder.BuildErrorFixPrompt(originalCode, errorsJson);

            var messages = new[]
            {
                new ChatMessage("user", $"以下のシェーダーのコンパイルエラーを修正してください。\n\nエラー:\n{errorsJson}")
            };

            string fixedCode = null;
            string apiError = null;

            yield return _client.SendMessageCoroutine(
                systemPrompt, messages, 4096,
                response => fixedCode = response,
                error => apiError = error
            );

            if (apiError != null)
            {
                onFailed?.Invoke($"API エラー: {apiError}");
                yield break;
            }

            if (!ShaderParser.TryExtractShaderCode(fixedCode, out var extracted))
            {
                onFailed?.Invoke("修正済みシェーダーコードを抽出できませんでした。");
                yield break;
            }

            var updatedPath = ShaderFileWriter.Update(shaderAssetPath, extracted);

            yield return null;
            yield return null;

            var remainingErrors = ShaderValidator.GetErrors(updatedPath);
            if (remainingErrors.Length == 0)
                onFixed?.Invoke(updatedPath);
            else
                onFailed?.Invoke($"修正後も {remainingErrors.Length} 件のエラーが残っています。");
        }
    }
}
