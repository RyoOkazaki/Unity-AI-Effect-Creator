using UnityEngine;

namespace AIShaderCreator.Editor
{
    public static class VFXParser
    {
        private const string BeginMarker = "VFX_BEGIN";
        private const string EndMarker = "VFX_END";

        public static bool TryExtractConfig(string response, out VFXConfig config)
        {
            config = null;
            var begin = response.IndexOf(BeginMarker);
            var end = response.IndexOf(EndMarker);
            if (begin < 0 || end < 0 || end <= begin) return false;

            var json = response.Substring(begin + BeginMarker.Length, end - begin - BeginMarker.Length).Trim();
            // Remove markdown code fence if present
            if (json.StartsWith("```")) json = json.Substring(json.IndexOf('\n') + 1);
            if (json.EndsWith("```")) json = json.Substring(0, json.LastIndexOf("```"));
            json = json.Trim();

            try
            {
                config = JsonUtility.FromJson<VFXConfig>(json);
                return config != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
