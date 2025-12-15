using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using AvatarDietToolkit.Editor.Analyzers;
using System.Collections.Generic;

namespace AvatarDietToolkit.Editor.Core
{
    public class AvatarScanner
    {
        private List<IAnalyzer> _analyzers;

        public AvatarScanner()
        {
            _analyzers = new List<IAnalyzer>();
            // Analyzers
            _analyzers.Add(new MeshAnalyzer());
            _analyzers.Add(new ParameterAnalyzer());
            _analyzers.Add(new TextureAnalyzer());
            _analyzers.Add(new PhysBoneAnalyzer());
        }

        public void RegisterAnalyzer(IAnalyzer analyzer)
        {
            _analyzers.Add(analyzer);
        }

        public DietReport Scan(GameObject avatarRoot)
        {
            DietReport report = new DietReport();

            if (avatarRoot == null)
            {
                Debug.LogError("[AvatarDiet] Avatar Root is null.");
                return report;
            }

            var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                Debug.LogError($"[AvatarDiet] Target '{avatarRoot.name}' does not have a VRCAvatarDescriptor.");
                // We might proceed anyway depending on strictness, but usually descriptor is required for Params/EyeLook etc.
                // let's create a warning instead of aborting fully? No, requirement says "Target Selection = Avatar Root(Descriptor exists)"
                return report;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < _analyzers.Count; i++)
            {
                var analyzer = _analyzers[i];
                EditorUtility.DisplayProgressBar("Avatar Diet", $"Analyzing {analyzer.GetType().Name}...", (float)(i + 1) / _analyzers.Count);
                try
                {
                    analyzer.Run(avatarRoot, report);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AvatarDiet] Analyzer failed: {ex}");
                }
            }
            EditorUtility.ClearProgressBar();

            sw.Stop();
            report.ScanDurationMs = sw.Elapsed.TotalMilliseconds;
            report.IsScanComplete = true;

            return report;
        }
    }
}
