using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public static class MaterialApplicator
    {
        private const string MaterialFolder = "Assets/AIGeneratedShaders/Materials";

        public static Material CreateAndSave(string shaderAssetPath, string materialName)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath);
            if (shader == null) return null;

            EnsureMaterialFolder();
            var sanitized = materialName.Replace(' ', '_');
            var matPath = $"{MaterialFolder}/{sanitized}.mat";

            var mat = new Material(shader) { name = sanitized };
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // 選択中のGameObjectのRendererに適用
        public static bool ApplyToSelectedObject(Material mat)
        {
            var go = Selection.activeGameObject;
            if (go == null) return false;
            return ApplyToGameObject(go, mat);
        }

        public static bool ApplyToGameObject(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return false;

            Undo.RecordObject(renderer, "Apply AI Generated Material");
            renderer.sharedMaterial = mat;
            EditorUtility.SetDirty(renderer);
            return true;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AIGeneratedShaders"))
                AssetDatabase.CreateFolder("Assets", "AIGeneratedShaders");
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/AIGeneratedShaders", "Materials");
        }
    }
}
