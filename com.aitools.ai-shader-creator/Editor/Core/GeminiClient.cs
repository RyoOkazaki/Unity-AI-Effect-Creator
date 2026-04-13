using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

namespace AIShaderCreator.Editor
{
    public class GeminiClient : IAIClient
    {
        private const string ApiEndpointFmt =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        public const string ModelFlash = "gemini-2.0-flash";
        public const string ModelPro = "gemini-1.5-pro";
        private const int TimeoutSeconds = 90;

        private readonly string _apiKey;
        private readonly string _model;

        public GeminiClient(string apiKey, string model = ModelFlash)
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
            var endpoint = string.Format(ApiEndpointFmt, _model);

            // Build Gemini request JSON
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"system_instruction\":{{\"parts\":[{{\"text\":{EscapeJson(systemPrompt)}}}]}},");
            sb.Append($"\"generationConfig\":{{\"maxOutputTokens\":{maxTokens}}},");
            sb.Append("\"contents\":[");
            for (int i = 0; i < messages.Length; i++)
            {
                if (i > 0) sb.Append(",");
                // Gemini uses "model" for assistant role
                var role = messages[i].role == "assistant" ? "model" : "user";
                sb.Append($"{{\"role\":{EscapeJson(role)},\"parts\":[{{\"text\":{EscapeJson(messages[i].content)}}}]}}");
            }
            sb.Append("]}");

            var bodyRaw = Encoding.UTF8.GetBytes(sb.ToString());

            using var webRequest = new UnityWebRequest(endpoint, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = TimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-goog-api-key", _apiKey);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    onSuccess?.Invoke(ParseGeminiText(webRequest.downloadHandler.text));
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

        // Extract candidates[0].content.parts[0].text
        private static string ParseGeminiText(string json)
        {
            const string textKey = "\"text\":";
            var idx = json.IndexOf(textKey, StringComparison.Ordinal);
            if (idx < 0) throw new Exception("No text in Gemini response");
            idx += textKey.Length;
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == ':')) idx++;
            if (idx >= json.Length || json[idx] != '"') throw new Exception("Expected string");
            idx++;
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
