using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using AvatarDietToolkit.Editor.Core;

namespace AvatarDietToolkit.Editor.Analyzers
{
    public class ParameterAnalyzer : IAnalyzer
    {
        public void Run(GameObject avatarRoot, DietReport report)
        {
            var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null || descriptor.expressionParameters == null)
            {
                return;
            }

            var paramsAsset = descriptor.expressionParameters;
            int totalBits = 0;
            int paramCount = 0;

            if (paramsAsset.parameters != null)
            {
                foreach (var p in paramsAsset.parameters)
                {
                    if (string.IsNullOrEmpty(p.name)) continue;
                    
                    int cost = 0;
                    switch (p.valueType)
                    {
                        case VRCExpressionParameters.ValueType.Int:
                            cost = 8;
                            break;
                        case VRCExpressionParameters.ValueType.Float:
                            cost = 8;
                            break;
                        case VRCExpressionParameters.ValueType.Bool:
                            cost = 1;
                            break;
                    }

                    totalBits += cost;
                    paramCount++;

                    // Log "Heavy" parameters? Not really a thing, but maybe we can list them if we want detail.
                }
            }

            report.Parameters.TotalSyncedBits = totalBits;
            report.Parameters.ParameterCount = paramCount;
            
            // Populate detail list if over budget to show culprits? 
            // Usually invalid to remove params easily without breaking controllers, so just reporting total is fine for now.
        }
    }
}
