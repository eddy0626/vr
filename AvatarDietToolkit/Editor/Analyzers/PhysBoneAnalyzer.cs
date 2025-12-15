using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using System.Collections.Generic;
using AvatarDietToolkit.Editor.Core;

namespace AvatarDietToolkit.Editor.Analyzers
{
    public class PhysBoneAnalyzer : IAnalyzer
    {
        public void Run(GameObject avatarRoot, DietReport report)
        {
            var pbs = avatarRoot.GetComponentsInChildren<VRCPhysBone>(true);
            var colliders = avatarRoot.GetComponentsInChildren<VRCPhysBoneCollider>(true);
            var contacts = avatarRoot.GetComponentsInChildren<VRCContactReceiver>(true);
            var senders = avatarRoot.GetComponentsInChildren<VRCContactSender>(true);

            report.PhysBones.ComponentCount = pbs.Length;
            report.PhysBones.ColliderCount = colliders.Length;
            report.PhysBones.ContactCount = contacts.Length + senders.Length; // Total Contacts

            // Calculate Affected Transforms
            HashSet<Transform> affected = new HashSet<Transform>();
            
            foreach (var pb in pbs)
            {
                Transform root = pb.rootTransform != null ? pb.rootTransform : pb.transform;
                HashSet<Transform> ignored = new HashSet<Transform>(pb.ignoreTransforms);
                
                // Recursive scan
                AddTransforms(root, ignored, affected);
                
                // Detailed check for lengthy chains
                int chainLen = CountChain(root, ignored);
                if (chainLen > 16) // soft warning
                {
                    report.PhysBones.DeepChains.Add(new IssueItem
                    {
                        ReferenceObject = pb.gameObject,
                        Name = pb.name,
                        Description = $"{chainLen} deep",
                        Value = chainLen,
                        Severity = DietStatus.Warning
                    });
                }
            }

            report.PhysBones.AffectedTransformCount = affected.Count;
        }

        private void AddTransforms(Transform current, HashSet<Transform> ignored, HashSet<Transform> affected)
        {
            if (current == null) return;
            if (ignored.Contains(current)) return;

            affected.Add(current);

            foreach (Transform child in current)
            {
                AddTransforms(child, ignored, affected);
            }
        }
        
        private int CountChain(Transform current, HashSet<Transform> ignored)
        {
            if (current == null) return 0;
            if (ignored.Contains(current)) return 0;
            
            int maxDepth = 0;
            foreach(Transform child in current)
            {
                int d = CountChain(child, ignored);
                if (d > maxDepth) maxDepth = d;
            }
            return 1 + maxDepth;
        }
    }
}
