using UnityEngine;
using AvatarDietToolkit.Editor.Core;

namespace AvatarDietToolkit.Editor.Analyzers
{
    public interface IAnalyzer
    {
        /// <summary>
        /// Runs analysis on the given avatar and populates the report.
        /// </summary>
        void Run(GameObject avatarRoot, DietReport report);
    }
}
