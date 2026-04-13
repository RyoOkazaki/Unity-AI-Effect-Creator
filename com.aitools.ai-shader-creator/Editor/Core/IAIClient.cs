using System;
using System.Collections;

namespace AIShaderCreator.Editor
{
    // 各AIサービスに共通のメッセージ形式
    public class ChatMessage
    {
        public string role;    // "user" or "assistant"
        public string content;

        public ChatMessage() { }
        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    // 全AIクライアントが実装する共通インターフェース
    public interface IAIClient
    {
        IEnumerator SendMessageCoroutine(
            string systemPrompt,
            ChatMessage[] messages,
            int maxTokens,
            Action<string> onSuccess,   // レスポンステキストをそのまま返す
            Action<string> onError);    // エラーメッセージ
    }
}
