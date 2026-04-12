using UnityEditor;

namespace AIShaderCreator.Editor
{
    public static class ApiKeyStorage
    {
        private const string PrefKey = "AIShaderCreator_ApiKey";
        private const byte XorKey = 0x5A;

        public static void Save(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorPrefs.DeleteKey(PrefKey);
                return;
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
            for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
            EditorPrefs.SetString(PrefKey, System.Convert.ToBase64String(bytes));
        }

        public static string Load()
        {
            var encoded = EditorPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(encoded)) return "";
            try
            {
                var bytes = System.Convert.FromBase64String(encoded);
                for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        public static bool HasKey() => !string.IsNullOrEmpty(Load());

        public static void Clear() => EditorPrefs.DeleteKey(PrefKey);
    }
}
