using System;
using UnityEngine;

namespace AIShaderCreator.Editor
{
    [Serializable]
    public class VFXConfig
    {
        public string effectType;       // fire, smoke, explosion, magic, sparkle, rain, snow, custom
        public string displayName;
        public float[] mainColor;       // RGBA 0-1
        public float[] secondaryColor;  // RGBA 0-1
        public float speed;
        public float scale;
        public float emissionRate;
        public float lifetime;
        public float spread;
        public float gravity;
        public float startSize;
        public bool looping;
        public float duration;

        public Color GetMainColor()
        {
            if (mainColor != null && mainColor.Length >= 4)
                return new Color(mainColor[0], mainColor[1], mainColor[2], mainColor[3]);
            return Color.white;
        }

        public Color GetSecondaryColor()
        {
            if (secondaryColor != null && secondaryColor.Length >= 4)
                return new Color(secondaryColor[0], secondaryColor[1], secondaryColor[2], secondaryColor[3]);
            return new Color(1f, 1f, 1f, 0f);
        }
    }
}
