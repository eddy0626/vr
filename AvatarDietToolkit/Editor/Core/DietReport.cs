using System.Collections.Generic;
using UnityEngine;

namespace AvatarDietToolkit.Editor.Core
{
    public enum DietStatus
    {
        Good,
        Warning,
        Poor
    }

    [System.Serializable]
    public class DietReport
    {
        public MeshStats Meshes = new MeshStats();
        public TextureStats Textures = new TextureStats();
        public ParameterStats Parameters = new ParameterStats();
        public PhysBoneStats PhysBones = new PhysBoneStats();
        
        public bool IsScanComplete = false;
        public double ScanDurationMs;
    }

    [System.Serializable]
    public class MeshStats
    {
        public int TotalTriangles;
        public int TotalMaterialSlots;
        public int TotalSkinnedMeshes;
        public int TotalUniqueBones;
        
        // Details
        public List<IssueItem> HighPolyMeshes = new List<IssueItem>();
        public List<IssueItem> ManyMaterialMeshes = new List<IssueItem>();
    }

    [System.Serializable]
    public class TextureStats
    {
        public long EstimatedTotalBytes;
        public int TotalTextureCount;
        
        // Details
        public List<TextureIssueItem> HeavyTextures = new List<TextureIssueItem>();
    }

    [System.Serializable]
    public class ParameterStats
    {
        public int TotalSyncedBits;
        public int ParameterCount;
        
        // Details
        public List<IssueItem> LargeParameters = new List<IssueItem>();
    }

    [System.Serializable]
    public class PhysBoneStats
    {
        public int ComponentCount;
        public int AffectedTransformCount;
        public int ColliderCount;
        public int ContactCount;
        
        // Details
        public List<IssueItem> ComponentList = new List<IssueItem>();
        public List<IssueItem> DeepChains = new List<IssueItem>();
    }

    [System.Serializable]
    public class IssueItem
    {
        public Object ReferenceObject;
        public string Name;
        public string Description; // e.g., "72,000 tris"
        public float Value; // For sorting
        public DietStatus Severity;
    }

    [System.Serializable]
    public class TextureIssueItem : IssueItem
    {
        public Texture TextureRef;
        public string OriginalRes; // "2048x2048"
        public string Format;      // "RGBA32"
        public long SizeBytes;
        public string ProposedChange; // "Resize to 1024"
    }
}
