using UnityEngine;
using System.Collections.Generic;
using AvatarDietToolkit.Editor.Core;

namespace AvatarDietToolkit.Editor.Analyzers
{
    public class MeshAnalyzer : IAnalyzer
    {
        public void Run(GameObject avatarRoot, DietReport report)
        {
            var renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
            
            int totalTris = 0;
            int totalSlots = 0;
            int totalSkinnedMeshes = 0;
            HashSet<Transform> uniqueBones = new HashSet<Transform>();

            foreach (var r in renderers)
            {
                // Skip EditorOnly, but usually we scan everything in the hierarchy
                // if (r.CompareTag("EditorOnly")) continue; 

                Mesh mesh = null;
                if (r is SkinnedMeshRenderer smr)
                {
                    mesh = smr.sharedMesh;
                    totalSkinnedMeshes++;
                    if (smr.bones != null)
                    {
                        foreach (var b in smr.bones)
                        {
                            if (b != null) uniqueBones.Add(b);
                        }
                    }
                }
                else if (r is MeshRenderer mr)
                {
                    var filter = r.GetComponent<MeshFilter>();
                    if (filter != null) mesh = filter.sharedMesh;
                }

                if (mesh != null)
                {
                    // Calculate tris efficiently
                    int indices = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        indices += (int)mesh.GetIndexCount(i);
                    }
                    int tris = indices / 3;
                    totalTris += tris;

                    // Log heavy meshes
                    if (tris > 5000) // Configurable threshold for "Heavy" detail
                    {
                        report.Meshes.HighPolyMeshes.Add(new IssueItem
                        {
                            ReferenceObject = r.gameObject,
                            Name = r.name,
                            Description = $"{tris:N0} tris",
                            Value = tris,
                            Severity = tris > 20000 ? DietStatus.Poor : DietStatus.Warning
                        });
                    }
                }

                // Material Slots
                var mats = r.sharedMaterials; // Allocate array
                int slotCount = mats.Length;
                // Count effective slots? VRC counts array length.
                totalSlots += slotCount;

                if (slotCount > 2) // Just a detailed logging threshold
                {
                    report.Meshes.ManyMaterialMeshes.Add(new IssueItem
                    {
                        ReferenceObject = r.gameObject,
                        Name = r.name,
                        Description = $"{slotCount} slots",
                        Value = slotCount,
                        Severity = slotCount > 4 ? DietStatus.Warning : DietStatus.Good
                    });
                }
            }

            report.Meshes.TotalTriangles = totalTris;
            report.Meshes.TotalMaterialSlots = totalSlots;
            report.Meshes.TotalSkinnedMeshes = totalSkinnedMeshes;
            report.Meshes.TotalUniqueBones = uniqueBones.Count;
        }
    }
}
