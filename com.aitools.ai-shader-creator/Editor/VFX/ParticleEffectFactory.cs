using UnityEditor;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    public static class ParticleEffectFactory
    {
        private const string OutputFolder = "Assets/GeneratedVFX";

        public static string CreatePrefab(VFXConfig config)
        {
            EnsureFolder();

            var go = new GameObject(config.displayName ?? "VFX_Effect");
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            ConfigureMain(ps, config);
            ConfigureEmission(ps, config);
            ConfigureShape(ps, config);
            ConfigureColorOverLifetime(ps, config);
            ConfigureSizeOverLifetime(ps, config);
            ConfigureVelocityByType(ps, config);
            ApplyMaterial(renderer);

            var prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{OutputFolder}/{SanitizeName(config.displayName ?? config.effectType ?? "VFX_Effect")}.prefab");

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefabPath;
        }

        private static void ConfigureMain(ParticleSystem ps, VFXConfig config)
        {
            var main = ps.main;
            main.loop = config.looping;
            main.duration = config.duration > 0 ? config.duration : 5f;
            main.startLifetime = config.lifetime > 0 ? config.lifetime : 2f;
            main.startSpeed = config.speed > 0 ? config.speed : 3f;
            main.startSize = config.startSize > 0 ? config.startSize : 0.3f;
            main.gravityModifier = config.gravity;
            main.startColor = config.GetMainColor();
            main.maxParticles = Mathf.Max(100, Mathf.RoundToInt(
                (config.emissionRate > 0 ? config.emissionRate : 50f) * main.startLifetime.constant));
            main.scalingMode = ParticleSystemScalingMode.Local;
        }

        private static void ConfigureEmission(ParticleSystem ps, VFXConfig config)
        {
            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 50f;

            // バースト設定（爆発系）
            if (IsExplosive(config.effectType))
            {
                emission.rateOverTime = 0;
                emission.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, Mathf.RoundToInt(config.emissionRate > 0 ? config.emissionRate : 200f))
                });
            }
        }

        private static void ConfigureShape(ParticleSystem ps, VFXConfig config)
        {
            var shape = ps.shape;
            shape.enabled = true;
            var spread = config.spread > 0 ? config.spread : 0.3f;

            switch (config.effectType?.ToLower())
            {
                case "fire":
                case "smoke":
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = Mathf.Clamp(spread * 60f, 5f, 60f);
                    shape.radius = 0.15f * (config.scale > 0 ? config.scale : 1f);
                    break;
                case "explosion":
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.3f * (config.scale > 0 ? config.scale : 1f);
                    break;
                case "rain":
                    shape.shapeType = ParticleSystemShapeType.Box;
                    var rainScale = config.scale > 0 ? config.scale : 1f;
                    shape.scale = new Vector3(8f * rainScale, 0.1f, 8f * rainScale);
                    break;
                case "snow":
                    shape.shapeType = ParticleSystemShapeType.Box;
                    var snowScale = config.scale > 0 ? config.scale : 1f;
                    shape.scale = new Vector3(10f * snowScale, 0.1f, 10f * snowScale);
                    break;
                case "magic":
                case "sparkle":
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = spread * (config.scale > 0 ? config.scale : 1f);
                    break;
                default:
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = spread;
                    break;
            }
        }

        private static void ConfigureColorOverLifetime(ParticleSystem ps, VFXConfig config)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;

            var mainC = config.GetMainColor();
            var secC = config.GetSecondaryColor();

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(mainC, 0f), new GradientColorKey(secC, 1f) },
                new[] { new GradientAlphaKey(mainC.a, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ConfigureSizeOverLifetime(ParticleSystem ps, VFXConfig config)
        {
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;

            AnimationCurve curve;
            switch (config.effectType?.ToLower())
            {
                case "fire":
                    curve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1.5f);
                    break;
                case "explosion":
                    curve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 2f);
                    break;
                case "smoke":
                    curve = AnimationCurve.Linear(0f, 0.5f, 1f, 2f);
                    break;
                default:
                    curve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 0f);
                    break;
            }
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ConfigureVelocityByType(ParticleSystem ps, VFXConfig config)
        {
            switch (config.effectType?.ToLower())
            {
                case "smoke":
                    var noise = ps.noise;
                    noise.enabled = true;
                    noise.strength = 0.5f;
                    noise.frequency = 0.5f;
                    noise.scrollSpeed = 0.3f;
                    break;
                case "magic":
                case "sparkle":
                    var magicNoise = ps.noise;
                    magicNoise.enabled = true;
                    magicNoise.strength = 1f;
                    magicNoise.frequency = 1f;
                    magicNoise.scrollSpeed = 1f;
                    break;
                case "rain":
                    var rainVelocity = ps.velocityOverLifetime;
                    rainVelocity.enabled = true;
                    rainVelocity.space = ParticleSystemSimulationSpace.World;
                    rainVelocity.y = new ParticleSystem.MinMaxCurve(-(config.speed > 0 ? config.speed : 10f));
                    break;
            }
        }

        private static void ApplyMaterial(ParticleSystemRenderer renderer)
        {
            var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (mat != null) renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static bool IsExplosive(string effectType) =>
            effectType?.ToLower() is "explosion" or "burst";

        private static string SanitizeName(string name) =>
            System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\- ]", "_");

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets", "GeneratedVFX");
        }
    }
}
