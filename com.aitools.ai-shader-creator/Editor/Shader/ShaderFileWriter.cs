using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public static class ShaderFileWriter
    {
        private const string OutputFolder = "Assets/AIGeneratedShaders";

        public static string Write(string shaderCode, string shaderName)
        {
            EnsureOutputFolder();
            var sanitized = SanitizeFileName(shaderName);
            var assetPath = $"{OutputFolder}/{sanitized}.shader";
            var absolutePath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);

            File.WriteAllText(absolutePath, shaderCode, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
            return assetPath;
        }

        public static string Update(string existingAssetPath, string newShaderCode)
        {
            // バックアップ作成
            var absolutePath = Path.Combine(Application.dataPath.Replace("Assets", ""), existingAssetPath);
            if (File.Exists(absolutePath))
                File.Copy(absolutePath, absolutePath + ".bak", overwrite: true);

            File.WriteAllText(absolutePath, newShaderCode, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(existingAssetPath);
            AssetDatabase.Refresh();
            return existingAssetPath;
        }

        private static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets", "AIGeneratedShaders");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
