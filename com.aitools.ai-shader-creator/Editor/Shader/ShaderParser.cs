using System.Text.RegularExpressions;

namespace AIShaderCreator.Editor
{
    public static class ShaderParser
    {
        private const string BeginMarker = "SHADER_BEGIN";
        private const string EndMarker = "SHADER_END";

        // SHADER_BEGIN〜SHADER_END マーカー間のコードを抽出
        public static bool TryExtractShaderCode(string apiResponse, out string shaderCode)
        {
            shaderCode = null;

            var beginIdx = apiResponse.IndexOf(BeginMarker);
            var endIdx = apiResponse.IndexOf(EndMarker);

            if (beginIdx < 0 || endIdx < 0 || endIdx <= beginIdx)
            {
                // マーカーがない場合、Shader "..." { ... } を正規表現で探す
                var match = Regex.Match(apiResponse, @"(Shader\s+""[^""]+""[\s\S]*?\}[\s\n]*\})", RegexOptions.Multiline);
                if (match.Success)
                {
                    shaderCode = match.Value.Trim();
                    return true;
                }
                return false;
            }

            // BeginMarker の後ろから抽出
            var start = beginIdx + BeginMarker.Length;
            // コードブロックの ``` を除去
            var raw = apiResponse.Substring(start, endIdx - start);
            raw = Regex.Replace(raw, @"^[\s`]+", "");   // 先頭の ``` と空白除去
            raw = Regex.Replace(raw, @"[\s`]+$", "");   // 末尾の ``` と空白除去
            shaderCode = raw.Trim();
            return !string.IsNullOrEmpty(shaderCode);
        }

        // Shader "Custom/Name" からシェーダー名を抽出
        public static string ExtractShaderName(string shaderCode)
        {
            var match = Regex.Match(shaderCode, @"Shader\s+""([^""]+)""");
            if (match.Success)
            {
                var full = match.Groups[1].Value; // "Custom/FireDissolve"
                var parts = full.Split('/');
                return parts[parts.Length - 1]; // "FireDissolve"
            }
            return "GeneratedShader";
        }
    }
}
