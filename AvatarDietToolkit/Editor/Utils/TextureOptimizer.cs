using UnityEngine;
using UnityEditor;

namespace AvatarDietToolkit.Editor.Utils
{
    public static class TextureOptimizer
    {
        public static void OptimizeTexture(Texture tex, int maxRes = 1024, bool enableCrunched = true)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path)) return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            // record undo? TextureImporter changes via code are hard to generic Undo.
            // But we can verify with user.
            // Usually, we just change settings. Editor allows Undo of inspector changes, but script changes might not register in Undo stack properly without SerializedObject.
            // Robust way: Use SerializedObject.
            
            SerializedObject so = new SerializedObject(importer);
            // But modifying TextureImporter needs 'SaveAndReimport'.
            
            // Simple approach for MVP
            importer.maxTextureSize = maxRes;
            importer.crunchedCompression = enableCrunched;
            importer.compressionQuality = 50; // Balanced
            importer.mipmapEnabled = true; // Always good for VRC
            
            // Standardize platform settings for PC
            // We'll set the Default settings or Standalone
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
            settings.overridden = true;
            settings.maxTextureSize = maxRes;
            settings.format = TextureImporterFormat.DXT5Crunched; // Usually safe for everything with alpha. BC7 is better but bigger.
            // Wait, BC7 doesn't support crunch. DXT5Crunched is standard for "Diet".
            settings.compressionQuality = 50;
            
            importer.SetPlatformTextureSettings(settings);
            
            importer.SaveAndReimport();
        }
    }
}
