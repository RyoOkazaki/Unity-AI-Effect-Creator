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

        // API送信用メッセージ（最新MaxApiMessages件 / userとassistantのみ）
        public ChatMessage[] ToApiMessages()
        {
            var result = new List<ChatMessage>();
            foreach (var msg in _messages)
            {
                if (msg.Role == MessageRole.User)
                    result.Add(new ChatMessage("user", msg.Content));
                else if (msg.Role == MessageRole.Assistant)
                    result.Add(new ChatMessage("assistant", msg.Content));
            }

            if (result.Count > MaxApiMessages)
                result = result.GetRange(result.Count - MaxApiMessages, MaxApiMessages);

            while (result.Count > 0 && result[0].role != "user")
                result.RemoveAt(0);

            return result.ToArray();
        }
    }
}
