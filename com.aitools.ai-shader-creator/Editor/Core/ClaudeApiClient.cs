using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AIShaderCreator.Editor
{
    [Serializable]
    public class ClaudeMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ClaudeRequest
    {
        public string model;
        public int max_tokens;
        public string system;
        public ClaudeMessage[] messages;
    }

    [Serializable]
    public class ClaudeResponseContent
    {
        public string type;
        public string text;
    }

    [Serializable]
    public class ClaudeResponse
    {
        public ClaudeResponseContent[] content;
        public string stop_reason;

        public string GetText()
        {
            if (content != null && content.Length > 0)
                return content[0].text ?? "";
            return "";
        }
    }

    [Serializable]
    public class ClaudeErrorResponse
    {
        public ClaudeError error;
    }

    [Serializable]
    public class ClaudeError
    {
        public string type;
        public string message;
    }

    public class ClaudeApiClient
    {
        private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";
        public const string ModelOpus = "claude-opus-4-6";
        public const string ModelHaiku = "claude-haiku-4-5-20251001";

        private readonly string _apiKey;
        private readonly string _model;
        private const int TimeoutSeconds = 90;

        public ClaudeApiClient(string apiKey, string model = ModelOpus)
        {
            _apiKey = apiKey;
            _model = model;
        }

        public IEnumerator SendMessageCoroutine(
            string systemPrompt,
            ClaudeMessage[] messages,
            int maxTokens,
            Action<ClaudeResponse> onSuccess,
            Action<string> onError)
        {
            var request = new ClaudeRequest
            {
                model = _model,
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = messages
            };

            var json = JsonUtility.ToJson(request);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(ApiEndpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = TimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", _apiKey);
            webRequest.SetRequestHeader("anthropic-version", ApiVersion);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var responseText = webRequest.downloadHandler.text;
                try
                {
                    var response = JsonUtility.FromJson<ClaudeResponse>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"レスポンスのパースに失敗しました: {e.Message}");
                }
            }
            else
            {
                var errorMsg = webRequest.error;
                if (!string.IsNullOrEmpty(webRequest.downloadHandler?.text))
                {
                    try
                    {
                        var errResp = JsonUtility.FromJson<ClaudeErrorResponse>(webRequest.downloadHandler.text);
                        if (errResp?.error != null)
                            errorMsg = $"API エラー ({errResp.error.type}): {errResp.error.message}";
                    }
                    catch { }
                }
                onError?.Invoke(errorMsg);
            }
        }
    }
}
