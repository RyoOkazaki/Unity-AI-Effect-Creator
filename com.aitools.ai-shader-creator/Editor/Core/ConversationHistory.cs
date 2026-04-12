using System.Collections.Generic;

namespace AIShaderCreator.Editor
{
    public enum MessageRole { User, Assistant, System }

    public class ConversationMessage
    {
        public MessageRole Role;
        public string Content;
        public bool IsError;

        public ConversationMessage(MessageRole role, string content, bool isError = false)
        {
            Role = role;
            Content = content;
            IsError = isError;
        }
    }

    public class ConversationHistory
    {
        private readonly List<ConversationMessage> _messages = new();
        private const int MaxApiMessages = 20;

        public IReadOnlyList<ConversationMessage> Messages => _messages;

        public void AddUserMessage(string content)
            => _messages.Add(new ConversationMessage(MessageRole.User, content));

        public void AddAssistantMessage(string content)
            => _messages.Add(new ConversationMessage(MessageRole.Assistant, content));

        public void AddErrorMessage(string content)
            => _messages.Add(new ConversationMessage(MessageRole.System, content, isError: true));

        public void Clear() => _messages.Clear();

        // Claude API送信用メッセージ（最新MaxApiMessages件 / userとassistantのみ）
        public ClaudeMessage[] ToApiMessages()
        {
            var result = new List<ClaudeMessage>();
            foreach (var msg in _messages)
            {
                if (msg.Role == MessageRole.User)
                    result.Add(new ClaudeMessage { role = "user", content = msg.Content });
                else if (msg.Role == MessageRole.Assistant)
                    result.Add(new ClaudeMessage { role = "assistant", content = msg.Content });
            }

            // 最新N件に絞る（APIトークン節約）
            if (result.Count > MaxApiMessages)
                result = result.GetRange(result.Count - MaxApiMessages, MaxApiMessages);

            // 最初のメッセージはuserである必要があるためチェック
            while (result.Count > 0 && result[0].role != "user")
                result.RemoveAt(0);

            return result.ToArray();
        }
    }
}
