using UnityEngine;
using UnityEditor;
using AvatarDietToolkit.Editor.Core;
using System.Collections.Generic;

// Safely handle missing VRC SDK
#if AVATAR_DIET_VRC_SDK_PRESENT
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.Contact.Components;
#endif

namespace AvatarDietToolkit.Editor.Windows
{
    public class AvatarDietWindow : EditorWindow
    {
        private int _currentTab = 3; // Default to PhysBones for Phase 3-3 verification
        private string[] _tabs = new string[] { "Overview", "Textures", "Parameters", "PhysBones" };
        private GameObject _targetAvatar;
        private Vector2 _scrollPos;

        // Cache
        private List<TextureInfo> _cachedTextures;

        // PhysBone Cache
        private List<VRCPhysBone> _cachedPhysBones;
        private List<VRCPhysBoneCollider> _cachedColliders;
        private List<Component> _cachedContacts; // Senders + Receivers

        [MenuItem(DietConstants.MenuPath)]
        public static void ShowWindow()
        {
            GetWindow<AvatarDietWindow>(DietConstants.ToolName);
        }

        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(10);
            _currentTab = GUILayout.Toolbar(_currentTab, _tabs);
            GUILayout.Space(10);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_currentTab)
            {
                case 0: DrawTab("Overview"); break;
                case 1: DrawTexturesTab(); break;
                case 2: DrawParametersTab(); break;
                case 3: DrawPhysBonesTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target Avatar:", GUILayout.Width(100));
            GameObject newTarget = (GameObject)EditorGUILayout.ObjectField(_targetAvatar, typeof(GameObject), true);

            if (newTarget != _targetAvatar)
            {
                _targetAvatar = newTarget;
                _cachedTextures = null;
                _cachedPhysBones = null;
                _cachedColliders = null;
                _cachedContacts = null;
            }
            GUILayout.EndHorizontal();

            if (_targetAvatar != null)
            {
                ValidateAvatar(_targetAvatar);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a GameObject to inspect.", MessageType.Info);
            }
        }

        private void ValidateAvatar(GameObject target)
        {
#if AVATAR_DIET_VRC_SDK_PRESENT
            var descriptor = target.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox("Selected object is not a valid Avatar (Missing VRCAvatarDescriptor).", MessageType.Warning);
            }
#else
            EditorGUILayout.HelpBox("VRChat SDK not detected. Please install VRChat SDK3 Avatars.", MessageType.Error);
#endif
        }

        private void DrawTab(string tabName)
        {
            EditorGUILayout.LabelField($"[{tabName}] Tab", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Not implemented", EditorStyles.centeredGreyMiniLabel);
        }

        // --- Phase 3-2: Textures Implementation ---
        private class TextureInfo
        {
            public Texture Ref;
            public string Name;
            public string AssetPath;
            public int OriginalWidth;
            public int OriginalHeight;
            public int MaxSize;
            public bool IsCrunched;
            public bool HasMipMaps;
            public bool IsTooLarge;
        }

        private void DrawTexturesTab()
        {
            if (_targetAvatar == null) return;

            if (GUILayout.Button("Scan Textures"))
            {
                ScanTextures();
            }

            if (_cachedTextures == null)
            {
                EditorGUILayout.HelpBox("Click Scan to analyze textures.", MessageType.Info);
                return;
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Found {_cachedTextures.Count} unique textures.", EditorStyles.boldLabel);

            foreach (var texInfo in _cachedTextures)
            {
                DrawTextureItem(texInfo);
            }
        }

        private void ScanTextures()
        {
            _cachedTextures = new List<TextureInfo>();
            var renderers = _targetAvatar.GetComponentsInChildren<Renderer>(true);
            HashSet<Texture> unique = new HashSet<Texture>();

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;

                    var shader = mat.shader;
                    int count = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < count; i++)
                    {
                        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            string propName = ShaderUtil.GetPropertyName(shader, i);
                            Texture t = mat.GetTexture(propName);
                            if (t != null) unique.Add(t);
                        }
                    }
                }
            }

            foreach (var t in unique)
            {
                string path = AssetDatabase.GetAssetPath(t);
                if (string.IsNullOrEmpty(path)) continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var settings = importer.GetPlatformTextureSettings("Standalone");

                int maxSize = settings.overridden ? settings.maxTextureSize : importer.maxTextureSize;
                bool crunched = settings.overridden ?
                    (settings.format == TextureImporterFormat.DXT5Crunched || settings.format == TextureImporterFormat.DXT1Crunched || settings.format == TextureImporterFormat.ETC2_RGBA8Crunched) :
                    importer.crunchedCompression;

                bool mips = importer.mipmapEnabled;

                _cachedTextures.Add(new TextureInfo
                {
                    Ref = t,
                    Name = t.name,
                    AssetPath = path,
                    OriginalWidth = t.width,
                    OriginalHeight = t.height,
                    MaxSize = maxSize,
                    IsCrunched = crunched,
                    HasMipMaps = mips,
                    IsTooLarge = maxSize >= 4096
                });
            }

            _cachedTextures.Sort((a, b) => b.MaxSize.CompareTo(a.MaxSize));
        }

        private void DrawTextureItem(TextureInfo info)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(info.Ref, GUILayout.Width(50), GUILayout.Height(50));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(info.Name, EditorStyles.boldLabel);
            if (info.IsTooLarge)
            {
                GUIStyle redStyle = new GUIStyle(EditorStyles.label);
                redStyle.normal.textColor = Color.red;
                redStyle.fontStyle = FontStyle.Bold;
                GUILayout.Label("TOO BIG", redStyle, GUILayout.Width(60));
            }
            GUILayout.EndHorizontal();
            string crunchStr = info.IsCrunched ? "Crunched" : "Uncompressed";
            string mipStr = info.HasMipMaps ? "Mips: On" : "Mips: OFF";
            EditorGUILayout.LabelField($"{info.OriginalWidth}x{info.OriginalHeight} -> Max: {info.MaxSize} | {crunchStr} | {mipStr}", EditorStyles.miniLabel);
            if (info.MaxSize >= 4096)
            {
                EditorGUILayout.HelpBox("4K Texture detected. Consider 2048 or lower.", MessageType.Warning);
            }
            if (!info.HasMipMaps && info.Ref is Texture2D)
            {
                GUIStyle orange = new GUIStyle(EditorStyles.miniLabel);
                orange.normal.textColor = new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField("Warning: No MipMaps (Costly at distance)", orange);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // --- Phase 3-1: Parameters Implementation ---
        private void DrawParametersTab()
        {
#if AVATAR_DIET_VRC_SDK_PRESENT
            if (_targetAvatar == null)
            {
                EditorGUILayout.HelpBox("No Avatar Selected.", MessageType.Warning);
                return;
            }

            var descriptor = _targetAvatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null) return;

            var expressionParams = descriptor.expressionParameters;
            if (expressionParams == null)
            {
                EditorGUILayout.HelpBox("No Expression Parameters Asset assigned in Descriptor.", MessageType.Error);
                return;
            }

            int totalBits = 0;
            int maxBits = 256;
            List<string> efficiencySuggestions = new List<string>();

            if (expressionParams.parameters != null)
            {
                foreach (var p in expressionParams.parameters)
                {
                    if (string.IsNullOrEmpty(p.name)) continue;
                    if (!p.networkSynced) continue;

                    int cost = 0;
                    if (p.valueType == VRCExpressionParameters.ValueType.Int || p.valueType == VRCExpressionParameters.ValueType.Float)
                    {
                        cost = 8;
                        efficiencySuggestions.Add($"'{p.name}' ({p.valueType}) uses 8 bits. Could it be a Bool (1 bit)?");
                    }
                    else if (p.valueType == VRCExpressionParameters.ValueType.Bool)
                    {
                        cost = 1;
                    }
                    totalBits += cost;
                }
            }
            
            EditorGUILayout.LabelField("Synced Parameter Memory", EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, (float)totalBits / maxBits, $"{totalBits} / {maxBits} bits");
            
            if (totalBits > maxBits)
            {
                EditorGUILayout.HelpBox($"OVER BUDGET! Exceeds {maxBits} bits. Parameters will not sync correctly.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Within Sync limits.", MessageType.Info);
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Optimization Suggestions", EditorStyles.boldLabel);
            if (efficiencySuggestions.Count > 0)
            {
                foreach (var tip in efficiencySuggestions)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(tip, EditorStyles.miniLabel);
                    GUILayout.Button("Check", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No obvious optimizations found.", EditorStyles.miniLabel);
            }
#else
            EditorGUILayout.LabelField("VRChat SDK Missing - Cannot analyze Parameters", EditorStyles.boldLabel);
#endif
        }

        // --- Phase 3-3: PhysBones Implementation ---
        private void DrawPhysBonesTab()
        {
#if AVATAR_DIET_VRC_SDK_PRESENT
            if (_targetAvatar == null) return;

            if (GUILayout.Button("Scan PhysBones"))
            {
                ScanPhysBones();
            }

            if (_cachedPhysBones == null)
            {
                EditorGUILayout.HelpBox("Click Scan to analyze PhysBones.", MessageType.Info);
                return;
            }

            // Summary
            DrawMetricBar("PhysBone Components", _cachedPhysBones.Count, DietConstants.MaxPhysBoneComponents); // 8
            DrawMetricBar("PhysBone Colliders", _cachedColliders.Count, DietConstants.MaxPhysBoneColliders); // 16
            DrawMetricBar("Contacts", _cachedContacts.Count, DietConstants.MaxContacts); // 16

            GUILayout.Space(10);

            // Lists
            DrawComponentList("PhysBone Components", _cachedPhysBones);
            DrawComponentList("Colliders", _cachedColliders);
            DrawComponentList("Contacts", _cachedContacts);

#else
            EditorGUILayout.LabelField("VRChat SDK Missing - Cannot analyze PhysBones", EditorStyles.boldLabel);
#endif
        }

#if AVATAR_DIET_VRC_SDK_PRESENT
        private void ScanPhysBones()
        {
            _cachedPhysBones = new List<VRCPhysBone>(_targetAvatar.GetComponentsInChildren<VRCPhysBone>(true));
            _cachedColliders = new List<VRCPhysBoneCollider>(_targetAvatar.GetComponentsInChildren<VRCPhysBoneCollider>(true));
            
            _cachedContacts = new List<Component>();
            _cachedContacts.AddRange(_targetAvatar.GetComponentsInChildren<VRCContactSender>(true));
            _cachedContacts.AddRange(_targetAvatar.GetComponentsInChildren<VRCContactReceiver>(true));
        }

        private void DrawMetricBar(string label, int current, int max)
        {
            EditorGUILayout.LabelField($"{label}: {current} / {max}");
            Rect rect = EditorGUILayout.GetControlRect(false, 18);
            
            float ratio = Mathf.Clamp01((float)current / max);
            
            // Color Logic
            Color originalColor = GUI.color;
            if (current > max) GUI.color = Color.red;
            else GUI.color = Color.green;

            EditorGUI.ProgressBar(rect, ratio, "");
            GUI.color = originalColor;

            if (current > max)
            {
                EditorGUILayout.HelpBox($"Too many {label}! (Limit: {max})", MessageType.Warning);
            }
        }

        private void DrawComponentList<T>(string title, List<T> items) where T : Component
        {
            if (items == null || items.Count == 0) return;

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var item in items)
            {
                if (item == null) continue;
                EditorGUILayout.ObjectField(GetHierarchyPath(item.transform), item, typeof(T), true);
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private string GetHierarchyPath(Transform t)
        {
            // Simple path relative to root? Or just name for now to save space
            // Let's do Name (Parent)
            if (t.parent != null) return $"{t.parent.name}/{t.name}";
            return t.name;
        }
#endif
    }
}
