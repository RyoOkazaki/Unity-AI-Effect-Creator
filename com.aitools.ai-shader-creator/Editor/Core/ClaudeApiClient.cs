using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

namespace AIShaderCreator.Editor
{
    [Serializable]
    class ClaudeReqMsg
    {
        public string role;
        public string content;
    }

    [Serializable]
    class ClaudeReq
    {
        public string model;
        public int max_tokens;
        public string system;
        public ClaudeReqMsg[] messages;
    }

    [Serializable]
    class ClaudeResContent
    {
        public string type;
        public string text;
    }

    [Serializable]
    class ClaudeRes
    {
        public ClaudeResContent[] content;
        public string stop_reason;
    }

    [Serializable]
    class ClaudeErrWrapper
    {
        public ClaudeErrDetail error;
    }

    [Serializable]
    class ClaudeErrDetail
    {
        public string type;
        public string message;
    }

    public class ClaudeApiClient : IAIClient
    {
        private const string ApiEndpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";
        public const string ModelOpus = "claude-opus-4-6";
        public const string ModelHaiku = "claude-haiku-4-5-20251001";
        private const int TimeoutSeconds = 90;

        private readonly string _apiKey;
        private readonly string _model;

        public ClaudeApiClient(string apiKey, string model = ModelOpus)
        {
            _apiKey = apiKey;
            _model = model;
        }

        public IEnumerator SendMessageCoroutine(
            string systemPrompt,
            ChatMessage[] messages,
            int maxTokens,
            Action<string> onSuccess,
            Action<string> onError)
        {
            var reqMessages = new ClaudeReqMsg[messages.Length];
            for (int i = 0; i < messages.Length; i++)
                reqMessages[i] = new ClaudeReqMsg { role = messages[i].role, content = messages[i].content };

            var request = new ClaudeReq
            {
                model = _model,
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = reqMessages
            };

            var json = UnityEngine.JsonUtility.ToJson(request);
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
                try
                {
                    var response = UnityEngine.JsonUtility.FromJson<ClaudeRes>(webRequest.downloadHandler.text);
                    var text = (response.content != null && response.content.Length > 0)
                        ? response.content[0].text ?? "" : "";
                    onSuccess?.Invoke(text);
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
                        var errResp = UnityEngine.JsonUtility.FromJson<ClaudeErrWrapper>(webRequest.downloadHandler.text);
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
