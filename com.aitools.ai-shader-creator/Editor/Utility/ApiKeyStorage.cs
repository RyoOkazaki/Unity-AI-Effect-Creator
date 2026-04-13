using UnityEditor;

namespace AIShaderCreator.Editor
{
    public static class ApiKeyStorage
    {
        private const string PrefKeyPrefix = "AIShaderCreator_ApiKey_";
        private const string LegacyPrefKey = "AIShaderCreator_ApiKey";
        private const byte XorKey = 0x5A;

        public static void Save(AIService service, string apiKey)
        {
            var key = PrefKeyPrefix + service.ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorPrefs.DeleteKey(key);
                return;
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
            for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
            EditorPrefs.SetString(key, System.Convert.ToBase64String(bytes));
        }

        public static string Load(AIService service)
        {
            var key = PrefKeyPrefix + service.ToString();
            var encoded = EditorPrefs.GetString(key, "");

            // 後方互換: Claudeの旧キーがあれば自動マイグレーション
            if (string.IsNullOrEmpty(encoded) && service == AIService.Claude)
            {
                var legacy = EditorPrefs.GetString(LegacyPrefKey, "");
                if (!string.IsNullOrEmpty(legacy))
                {
                    EditorPrefs.SetString(key, legacy);
                    EditorPrefs.DeleteKey(LegacyPrefKey);
                    encoded = legacy;
                }
            }

            if (string.IsNullOrEmpty(encoded)) return "";
            try
            {
                var bytes = System.Convert.FromBase64String(encoded);
                for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch { return ""; }
        }

        public static bool HasKey(AIService service) => !string.IsNullOrEmpty(Load(service));

        public static void Clear(AIService service)
            => EditorPrefs.DeleteKey(PrefKeyPrefix + service.ToString());

        public static void ClearAll()
        {
            foreach (AIService s in System.Enum.GetValues(typeof(AIService)))
                Clear(s);
        }

        // 旧APIとの互換性（ClaudeのみのHasKey/Load/Save/Clear）
        public static bool HasKey() => HasKey(AIService.Claude);
        public static string Load() => Load(AIService.Claude);
        public static void Save(string apiKey) => Save(AIService.Claude, apiKey);
        public static void Clear() => Clear(AIService.Claude);
    }
}
