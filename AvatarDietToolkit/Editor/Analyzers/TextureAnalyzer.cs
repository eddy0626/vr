using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AvatarDietToolkit.Editor.Core;

namespace AvatarDietToolkit.Editor.Analyzers
{
    public class TextureAnalyzer : IAnalyzer
    {
        public void Run(GameObject avatarRoot, DietReport report)
        {
            var renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
            HashSet<Texture> uniqueTextures = new HashSet<Texture>();
            
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    
                    // Iterate generic shader properties
                    var shader = mat.shader;
                    int count = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < count; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            string propName = ShaderUtil.GetPropertyName(shader, i);
                            Texture t = mat.GetTexture(propName);
                            if (t != null) uniqueTextures.Add(t);
                        }
                    }
                }
            }

            long totalBytes = 0;
            
            foreach (var tex in uniqueTextures)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                if (string.IsNullOrEmpty(path)) continue; // Builtin texture

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
                if (!settings.overridden)
                {
                    // Fallback to default
                    // Note: This is complex because default might be Android if switched? 
                    // Assuming we are in PC Edit mode.
                    // If not overridden, it uses the "Default" tab settings.
                    // We can just look at the texture itself?
                    // No, texture.width might be the imported size, which is good.
                    // But Format is tricky. 
                }

                int width = tex.width;
                int height = tex.height;
                // If we can get the actual format from the texture object in Editor:
                TextureFormat format = TextureFormat.RGBA32;
                if (tex is Texture2D t2d) format = t2d.format;
                
                long bytes = EstimateBytes(width, height, format, true); // Assuming MipMaps enabled
                
                totalBytes += bytes;

                // Detail Logging
                if (bytes > 5 * 1024 * 1024) // > 5MB
                {
                    report.Textures.HeavyTextures.Add(new TextureIssueItem
                    {
                        Name = tex.name,
                        ReferenceObject = tex,
                        OriginalRes = $"{width}x{height}",
                        Format = format.ToString(),
                        SizeBytes = bytes,
                        Description = $"{FormatBytes(bytes)}",
                        Severity = bytes > 20 * 1024 * 1024 ? DietStatus.Poor : DietStatus.Warning
                    });
                }
            }

            report.Textures.EstimatedTotalBytes = totalBytes;
            report.Textures.TotalTextureCount = uniqueTextures.Count;
        }

        private long EstimateBytes(int w, int h, TextureFormat fmt, bool mips)
        {
            float bpp = 4.0f; // Default RGBA32
            
            // Rough approximation
            switch(fmt)
            {
                case TextureFormat.DXT1: // BC1
                case TextureFormat.BC4:
                    bpp = 0.5f; // 4 bits
                    break;
                case TextureFormat.DXT5: // BC3
                case TextureFormat.BC5:
                case TextureFormat.BC7: // Actually BC7 is 1 byte/pixel (8 bits) equivalent usually? No, BC7 is 8 bits/pixel (16 bytes per 4x4 block).
                    bpp = 1.0f; // 8 bits
                    break;
                case TextureFormat.RGB24:
                    bpp = 3.0f;
                    break;
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    bpp = 4.0f;
                    break;
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    bpp = 1.0f; 
                    break;
                // Add more as needed
            }
            
            long baseBytes = (long)(w * h * bpp);
            return mips ? (long)(baseBytes * 1.33f) : baseBytes;
        }

        private string FormatBytes(long bytes)
        {
            return $"{bytes / 1024.0f / 1024.0f:F1} MB";
        }
    }
}
