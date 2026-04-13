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

            switch (config.effectType?.ToLower())
            {
                case "fire":      ConfigureFire(ps, renderer, config);      break;
                case "smoke":     ConfigureSmoke(ps, renderer, config);     break;
                case "explosion": ConfigureExplosion(ps, renderer, config); break;
                case "magic":
                case "sparkle":   ConfigureMagic(ps, renderer, config);     break;
                case "rain":      ConfigureRain(ps, renderer, config);      break;
                case "snow":      ConfigureSnow(ps, renderer, config);      break;
                default:          ConfigureDefault(ps, renderer, config);   break;
            }

            var prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{OutputFolder}/{SanitizeName(config.displayName ?? config.effectType ?? "VFX_Effect")}.prefab");
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefabPath;
        }

        // ============================================================
        // FIRE
        // ============================================================
        private static void ConfigureFire(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float intensity = config.speed > 0 ? Mathf.Clamp(config.speed / 3f, 0.5f, 5f) : 1f;
            float scale     = config.scale > 0 ? config.scale : 1f;
            var   mainColor = config.mainColor != null ? config.GetMainColor() : new Color(1f, 0.45f, 0.05f);

            var main = ps.main;
            main.loop           = true;
            main.duration       = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.2f * scale);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2f * intensity, 4.5f * intensity);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.28f * scale);
            main.startRotation  = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.08f;
            main.maxParticles   = Mathf.RoundToInt(600 * intensity * scale);

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 160f * intensity;

            var shape = ps.shape;
            shape.enabled    = true;
            shape.shapeType  = ParticleSystemShapeType.Cone;
            shape.angle      = Mathf.Clamp(10f + (config.spread > 0 ? config.spread * 25f : 5f), 5f, 40f);
            shape.radius     = 0.12f * scale;

            // 上昇しながら減速
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, 0f));

            // 炎の色変化: 明るい芯 → オレンジ → 赤 → 消える
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.Lerp(mainColor, Color.yellow, 0.6f), 0f),
                    new GradientColorKey(mainColor, 0.25f),
                    new GradientColorKey(Color.Lerp(mainColor, Color.red, 0.6f), 0.65f),
                    new GradientColorKey(new Color(0.15f, 0f, 0f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.75f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            // 立ち上がって大きくなり、先端で消える
            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f,    0.2f, 0f,  4f),
                new Keyframe(0.2f,  1f),
                new Keyframe(0.75f, 0.8f),
                new Keyframe(1f,    0.05f)));

            // 揺らぎノイズ（炎らしい乱流）
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = 0.55f * intensity;
            noise.frequency   = 0.9f;
            noise.scrollSpeed = 0.5f * intensity;
            noise.quality     = ParticleSystemNoiseQuality.Medium;

            // パーティクルを回転させてランダム感を出す
            var rotOL = ps.rotationOverLifetime;
            rotOL.enabled = true;
            rotOL.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode   = ParticleSystemSortMode.YoungestInFront;
            ApplyAdditiveMaterial(renderer);
        }

        // ============================================================
        // SMOKE
        // ============================================================
        private static void ConfigureSmoke(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float scale = config.scale > 0 ? config.scale : 1f;
            var   color = config.mainColor != null ? config.GetMainColor() : new Color(0.4f, 0.4f, 0.4f);

            var main = ps.main;
            main.loop            = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(3f * scale, 6f * scale);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.3f * scale, 0.8f * scale);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f;
            main.maxParticles    = 200;

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 20f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 15f;
            shape.radius    = 0.1f * scale;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.15f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 3f));

            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = 1.2f;
            noise.frequency   = 0.3f;
            noise.scrollSpeed = 0.2f;

            var rotOL = ps.rotationOverLifetime;
            rotOL.enabled = true;
            rotOL.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyAlphaMaterial(renderer);
        }

        // ============================================================
        // EXPLOSION
        // ============================================================
        private static void ConfigureExplosion(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float intensity = config.speed > 0 ? config.speed / 3f : 1f;
            float scale     = config.scale > 0 ? config.scale : 1f;
            var   color     = config.mainColor != null ? config.GetMainColor() : new Color(1f, 0.5f, 0.1f);

            var main = ps.main;
            main.loop            = false;
            main.duration        = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.5f * scale);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3f * intensity, 10f * intensity);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.1f * scale, 0.6f * scale);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.3f;
            main.maxParticles    = 500;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(300 * scale)) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f * scale;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.yellow, 0.1f),
                    new GradientColorKey(color, 0.4f),
                    new GradientColorKey(new Color(0.15f, 0f, 0f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyAdditiveMaterial(renderer);
        }

        // ============================================================
        // MAGIC / SPARKLE
        // ============================================================
        private static void ConfigureMagic(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float scale = config.scale > 0 ? config.scale : 1f;
            var   color = config.mainColor != null ? config.GetMainColor() : new Color(0.5f, 0.3f, 1f);
            var   color2 = config.secondaryColor != null ? config.GetSecondaryColor() : new Color(0.8f, 0.5f, 1f);

            var main = ps.main;
            main.loop            = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.02f * scale, 0.1f * scale);
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.15f;
            main.maxParticles    = 400;

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 80f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = (config.spread > 0 ? config.spread : 0.5f) * scale;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color2, 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(color2, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0f, 1f, 0f));

            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = 0.8f;
            noise.frequency   = 1.2f;
            noise.scrollSpeed = 0.8f;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyAdditiveMaterial(renderer);
        }

        // ============================================================
        // RAIN
        // ============================================================
        private static void ConfigureRain(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float scale     = config.scale > 0 ? config.scale : 1f;
            float intensity = config.speed > 0 ? config.speed : 8f;

            var main = ps.main;
            main.loop            = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = 1.5f;
            main.startSpeed      = intensity;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.gravityModifier = 1f;
            main.maxParticles    = 2000;
            main.startColor      = config.mainColor != null ? config.GetMainColor() : new Color(0.7f, 0.85f, 1f, 0.6f);

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 300f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(8f * scale, 0.1f, 8f * scale);

            renderer.renderMode  = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
            ApplyAlphaMaterial(renderer);
        }

        // ============================================================
        // SNOW
        // ============================================================
        private static void ConfigureSnow(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            float scale = config.scale > 0 ? config.scale : 1f;

            var main = ps.main;
            main.loop            = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            main.gravityModifier = 0.05f;
            main.maxParticles    = 1000;
            main.startColor      = config.mainColor != null ? config.GetMainColor() : new Color(1f, 1f, 1f, 0.85f);

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 60f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(10f * scale, 0.1f, 10f * scale);

            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = 0.3f;
            noise.frequency   = 0.3f;
            noise.scrollSpeed = 0.1f;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyAlphaMaterial(renderer);
        }

        // ============================================================
        // DEFAULT (custom)
        // ============================================================
        private static void ConfigureDefault(ParticleSystem ps, ParticleSystemRenderer renderer, VFXConfig config)
        {
            var main = ps.main;
            main.loop            = config.looping;
            main.duration        = config.duration > 0 ? config.duration : 5f;
            main.startLifetime   = config.lifetime > 0 ? config.lifetime : 2f;
            main.startSpeed      = config.speed > 0 ? config.speed : 3f;
            main.startSize       = config.startSize > 0 ? config.startSize : 0.2f;
            main.gravityModifier = config.gravity;
            main.startColor      = config.GetMainColor();
            main.maxParticles    = 500;

            var emission = ps.emission;
            emission.rateOverTime = config.emissionRate > 0 ? config.emissionRate : 50f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = config.spread > 0 ? config.spread : 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(config.GetMainColor(), 0f), new GradientColorKey(config.GetSecondaryColor(), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyAlphaMaterial(renderer);
        }

        // ============================================================
        // MATERIAL HELPERS
        // ============================================================
        private static void ApplyAdditiveMaterial(ParticleSystemRenderer renderer)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Additive")
                      ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                return;
            }

            var mat = new Material(shader) { name = "Particle_Additive" };

            // URP Particles/Unlit の Additive ブレンド設定
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Blend", 2f); // Additive = 2
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            renderer.material = mat;
        }

        private static void ApplyAlphaMaterial(ParticleSystemRenderer renderer)
        {
            var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (mat != null) renderer.material = mat;
        }

        private static string SanitizeName(string name) =>
            System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\- ]", "_").Trim();

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets", "GeneratedVFX");
        }
    }
}
