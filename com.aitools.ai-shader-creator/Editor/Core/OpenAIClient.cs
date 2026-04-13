using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

namespace AIShaderCreator.Editor
{
    public class OpenAIClient : IAIClient
    {
        private const string ApiEndpoint = "https://api.openai.com/v1/chat/completions";
        public const string ModelGpt4o = "gpt-4o";
        public const string ModelGpt4oMini = "gpt-4o-mini";
        private const int TimeoutSeconds = 90;

        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIClient(string apiKey, string model = ModelGpt4o)
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
            // Build JSON manually (JsonUtility doesn't support anonymous types)
            var sb = new StringBuilder();
            sb.Append($"{{\"model\":{EscapeJson(_model)},\"max_tokens\":{maxTokens},\"messages\":[");
            sb.Append($"{{\"role\":\"system\",\"content\":{EscapeJson(systemPrompt)}}}");
            foreach (var msg in messages)
                sb.Append($",{{\"role\":{EscapeJson(msg.role)},\"content\":{EscapeJson(msg.content)}}}");
            sb.Append("]}");

            var bodyRaw = Encoding.UTF8.GetBytes(sb.ToString());

            using var webRequest = new UnityWebRequest(ApiEndpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = TimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {_apiKey}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    onSuccess?.Invoke(ParseContentFromJson(webRequest.downloadHandler.text, "\"content\":"));
                }
                catch (Exception e)
                {
                    onError?.Invoke($"レスポンスのパースに失敗しました: {e.Message}");
                }
            }
            else
            {
                onError?.Invoke(ExtractApiError(webRequest));
            }
        }

        // Extract the first string value after the given key in JSON
        private static string ParseContentFromJson(string json, string key)
        {
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) throw new Exception("Key not found: " + key);
            idx += key.Length;
            // Skip whitespace and colon
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == ':')) idx++;
            if (idx >= json.Length || json[idx] != '"') throw new Exception("Expected string value");
            idx++; // skip opening quote
            return ReadJsonString(json, ref idx);
        }

        private static string ReadJsonString(string json, ref int idx)
        {
            var sb = new StringBuilder();
            while (idx < json.Length)
            {
                var c = json[idx++];
                if (c == '"') break;
                if (c == '\\' && idx < json.Length)
                {
                    var esc = json[idx++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string ExtractApiError(UnityWebRequest req)
        {
            var msg = req.error;
            var body = req.downloadHandler?.text;
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var msgStart = body.IndexOf("\"message\":\"", StringComparison.Ordinal) + 11;
                    if (msgStart > 10)
                    {
                        var msgEnd = body.IndexOf('"', msgStart);
                        if (msgEnd > msgStart)
                            msg = $"API エラー: {body.Substring(msgStart, msgEnd - msgStart)}";
                    }
                }
                catch { }
            }
            return msg;
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
