using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public class ShaderError
    {
        public int Line;
        public string Message;
        public string Platform;

        public override string ToString() => $"Line {Line}: {Message}";
    }

    public static class ShaderValidator
    {
        public static ShaderError[] GetErrors(string assetPath)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null) return Array.Empty<ShaderError>();
            return GetErrors(shader);
        }

        public static ShaderError[] GetErrors(Shader shader)
        {
            if (shader == null) return Array.Empty<ShaderError>();

            var errors = new List<ShaderError>();
            int count = ShaderUtil.GetShaderMessageCount(shader);
            for (int i = 0; i < count; i++)
            {
                var msg = ShaderUtil.GetShaderMessage(shader, i);
                if (msg.severity == ShaderCompilerMessageSeverity.Error)
                {
                    errors.Add(new ShaderError
                    {
                        Line = msg.line,
                        Message = msg.message,
                        Platform = msg.platform.ToString()
                    });
                }
            }
            return errors.ToArray();
        }

        public static bool HasErrors(string assetPath) => GetErrors(assetPath).Length > 0;

        public static string ToJson(ShaderError[] errors)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int i = 0; i < errors.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{{\"line\":{errors[i].Line},\"message\":\"{EscapeJson(errors[i].Message)}\"}}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }
}
