using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public class MaterialRenderingAnalysis : CustomEditorWindow
    {
        private Renderer[] _renderers;
        private int _rendererCount;
        private int _materialCount;
        private int _uniqueShaderCount;

        private Vector2 _scrollPosition;

        private Renderer[] _snapShotRenderers;

        private IEnumerable<KeyValuePair<Shader, int>> _shaderOccurrence = null;
        private IEnumerable<KeyValuePair<Material, int>> _materialOccurence = null;

        private Dictionary<Material, GameObject[]> _materialUses = new Dictionary<Material,
            GameObject[]>();

        private Dictionary<Shader, KeyValuePair<Material, int>[]> _shaderUses = new Dictionary<Shader,
            KeyValuePair<Material, int>[]>();

        private GUILayoutOption[][] _columnOptions;

        [MenuItem(HotspotWindowHelper.ANALYSIS_PREFIX + "Rendered Material Analysis", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(MaterialRenderingAnalysis));
            win.titleContent = new GUIContent("Rendered Material Analysis");
        }

        protected override void Initialize()
        {
            base.Initialize();
            RefreshMeshRendererCache();

            _columnOptions = new GUILayoutOption[][]
            {
                new GUILayoutOption[]
                    { GUILayout.Height(EditorGUIUtility.singleLineHeight) },
                new GUILayoutOption[]
                    { GUILayout.Height(EditorGUIUtility.singleLineHeight) }
            };
        }

        private void RefreshMeshRendererCache()
        {
            _renderers = GameObject.FindObjectsOfType<Renderer>();
        }

        protected override void OnGUI()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            CustomEditorGUI.Title("General Info", EditorStyles.boldLabel);

            GUILayout.Label($"Renderers: {_rendererCount}");
            GUILayout.Label($"Materials: {_materialCount}");
            GUILayout.Label($"Unique Shaders: {_uniqueShaderCount}");

            if (GUILayout.Button("Take Material Snapshot"))
            {
                TakeMaterialSnapshot();
            }

            if (_shaderOccurrence != null)
            {
                RenderShaderInfo();
                RenderMaterialInfo();
            }

            GUILayout.EndScrollView();
        }

        private void RenderMaterialInfo()
        {
            GUILayout.Space(10f);
            CustomEditorGUI.Title("Occurrence of Materials:", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            foreach (var material in _materialOccurence)
            {
                EditorGUILayout.BeginHorizontal();
                bool isOpen = _materialUses.ContainsKey(material.Key);

                string iconName = isOpen ? "Fa_AngleUp" : "Fa_AngleDown";
                if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon(iconName)))
                {
                    // If the icon is open                        
                    if (isOpen)
                        _materialUses.Remove(material.Key);
                    else
                        GetMaterialUses(material.Key);

                    // Toggle open status
                    isOpen = !isOpen;
                }

                // Display shader name and its occurrence
                EditorGUILayout.LabelField($"{material.Key.name}: {material.Value}");

                using (new eUtility.DisabledGroup())
                {
                    EditorGUILayout.ObjectField(material.Key, typeof(UnityEngine.Object), false);
                }

                EditorGUILayout.EndHorizontal();

                if (isOpen)
                {
                    // Display shades usages
                    GUILayout.Space(10);
                    using (var table = new eUtility.SimpleTableView(new[] { "GameObject name", "GameObject path" },
                               _columnOptions))
                    {
                        var uses = _materialUses[material.Key];
                        foreach (var gameObject in uses)
                            table.DrawRow(gameObject.name, gameObject.GetFullName());
                    }
                }
            }
        }


        private void RenderShaderInfo()
        {
            GUILayout.Space(5f);
            CustomEditorGUI.Title("Occurrence of Shaders:", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            foreach (var shader in _shaderOccurrence)
            {
                EditorGUILayout.BeginHorizontal();
                bool isOpen = _shaderUses.ContainsKey(shader.Key);

                string iconName = isOpen ? "Fa_AngleUp" : "Fa_AngleDown";
                if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon(iconName)))
                {
                    // If the icon is open                        
                    if (isOpen)
                        _shaderUses.Remove(shader.Key);
                    else
                        GetShaderUses(shader.Key);

                    // Toggle open status
                    isOpen = !isOpen;
                }

                // Display shader name and its occurrence
                EditorGUILayout.LabelField($"{shader.Key.name}: {shader.Value}");

                using (new eUtility.DisabledGroup())
                {
                    EditorGUILayout.ObjectField(shader.Key, typeof(UnityEngine.Object), false);
                }

                EditorGUILayout.EndHorizontal();

                if (isOpen)
                {
                    // Display shades usages
                    GUILayout.Space(10);
                    var uses = _shaderUses[shader.Key];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Material name", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Amount of uses", EditorStyles.boldLabel);
                    GUILayout.EndHorizontal();

                    foreach (var materialUsePair in uses)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{materialUsePair.Key.name}");
                        EditorGUILayout.LabelField($"{materialUsePair.Value}");
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(10);
                }
            }
        }

        private void GetShaderUses(Shader shaderKey)
        {
            var materials = _snapShotRenderers.SelectMany(x => x.sharedMaterials).ToArray();
            var materialsWithShaderInstance = materials.Where(m => m.shader.name == shaderKey.name).ToArray();
            var occurencePairs = materialsWithShaderInstance
                .GroupBy(m => m)
                .Select(g => new KeyValuePair<Material, int>(g.Key, g.Count())).ToArray();
            occurencePairs.SortByDescending(x => x.Value);
            _shaderUses.Add(shaderKey, occurencePairs);
        }

        private void GetMaterialUses(Material material)
        {
            var gameObjectsWithTargetMaterial =
                _snapShotRenderers.Where(r => r.sharedMaterials.Any(m => m == material)).Select(r => r.gameObject)
                    .ToArray();
            _materialUses.Add(material, gameObjectsWithTargetMaterial);
        }

        


        private void TakeMaterialSnapshot()
        {
            // TODO: Get the main camera through Magnus camera service
            Camera mainCamera = Camera.main;

            // Get the renderers that are visible and in the frustum of the main camera
            _snapShotRenderers = _renderers.Where(x => x.isVisible).ToArray();
            _snapShotRenderers = _snapShotRenderers.Where(r => r.IsWithinFrustum(mainCamera)).ToArray();

            // From these renderers, get all the shared materials into one array.
            var materials = _snapShotRenderers.SelectMany(x => x.sharedMaterials).ToArray();

            // Find out all unique shaders and names used in these materials.
            var uniqueShaders = materials.Select(x => x.shader).Distinct().ToArray();
            var uniqueShaderNames = uniqueShaders.Select(x => x.name).Distinct().ToArray();

            // Update the count of total renderers, total materials and total unique shaders.
            _rendererCount = _snapShotRenderers.Length;
            _materialCount = materials.Length;
            _uniqueShaderCount = uniqueShaderNames.Length;

            // Group the materials by themselves to get the occurrence of each material.
            // Sort the material occurrence data in descending order.
            _materialOccurence = materials
                .GroupBy(m => m)
                .Select(g => new KeyValuePair<Material, int>(g.Key, g.Count()));
            _materialOccurence = _materialOccurence.OrderByDescending(x => x.Value);

            // Group the materials by shader name to get the occurrence of each shader.
            // Sort the shader occurrence data in descending order.
            _shaderOccurrence = materials.GroupBy(x => x.shader.name)
                .Select(x => new KeyValuePair<Shader, int>(x.First().shader, x.Count()));
            _shaderOccurrence = _shaderOccurrence.OrderByDescending(x => x.Value);
        }
    }
}