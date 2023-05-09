using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Magnus;
using Rhinox.Perceptor;
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

        private Dictionary<Material, KeyValuePair<Mesh, GameObject>[]> _materialUses = new();

        private Dictionary<Shader, KeyValuePair<Material, int>[]> _shaderUses = new();

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
        }

        private void RefreshMeshRendererCache()
        {
            _renderers = FindObjectsOfType<Renderer>();
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
            RenderInfo(_materialOccurence, _materialUses, GetMaterialUses, "Occurrence of Materials:");
        }

        private void RenderShaderInfo()
        {
            RenderInfo(_shaderOccurrence, _shaderUses, GetShaderUses, "Occurrence of Shaders:");
        }

        private void RenderInfo<T, TU>(IEnumerable<KeyValuePair<T, int>> occurrence, IDictionary<T, TU> uses,
            Action<T> getUses, string paragraphTitle)
        {
            if (typeof(T) != typeof(Material) && typeof(T) != typeof(Shader))
            {
                PLog.Warn<HotspotLogger>(
                    $"[MaterialRenderingAnalysis,RenderInfo], function is not implemented for type {typeof(T)}");
                return;
            }

            GUILayout.Space(5f);
            CustomEditorGUI.Title(paragraphTitle, EditorStyles.boldLabel);
            GUILayout.Space(10f);

            foreach (var item in occurrence)
            {
                EditorGUILayout.BeginHorizontal();
                bool isOpen = uses.ContainsKey(item.Key);

                string iconName = isOpen ? "Fa_AngleUp" : "Fa_AngleDown";
                if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon(iconName)))
                {
                    // If the icon is open                        
                    if (isOpen)
                        uses.Remove(item.Key);
                    else
                        getUses(item.Key);

                    // Toggle open status
                    isOpen = !isOpen;
                }

                Material mat = item.Key as Material;
                Shader shader = item.Key as Shader;

                // Display name and its occurrence
                if (mat != null)
                    EditorGUILayout.LabelField($"{mat.name}: {item.Value}");
                else if (shader != null)
                    EditorGUILayout.LabelField($"{shader.name}: {item.Value}");

                using (new eUtility.DisabledGroup())
                {
                    if (mat != null)
                        EditorGUILayout.ObjectField(mat, typeof(Material), false);
                    else if (shader != null)
                        EditorGUILayout.ObjectField(shader, typeof(Shader), false);
                }

                EditorGUILayout.EndHorizontal();

                if (isOpen)
                {
                    // Display usages
                    GUILayout.Space(10);
                    if (mat != null)
                    {
                        DisplayMaterialInfo(mat);
                        DisplayMaterialUses(_materialUses[mat]);
                    }
                    else if (shader != null)
                    {
                        DisplayShaderInfo(shader);
                        DisplayShaderUses(_shaderUses[shader]);
                    }

                    GUILayout.Space(10);
                }
            }
        }

        private void DisplayShaderInfo(Shader shader)
        {
            GUILayout.Label("Shader Info", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Amount of passes: ");
            EditorGUILayout.LabelField(shader.passCount.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Amount of keywords: ");
            EditorGUILayout.LabelField(shader.keywordSpace.keywordCount.ToString());
            GUILayout.EndHorizontal();
        }

        private void DisplayMaterialInfo(Material mat)
        {
            GUILayout.Label("Material Info", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Shader: ");
            EditorGUILayout.LabelField(mat.shader.name);
            GUILayout.EndHorizontal();

            foreach (string keywordName in mat.shaderKeywords)
            {
                GUILayout.Label(keywordName, EditorStyles.miniLabel);
            }
            
        }

        private void DisplayMaterialUses(IEnumerable<KeyValuePair<Mesh, GameObject>> meshGameObjectPairs)
        {
            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Applied Mesh", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GameObject Full name", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            foreach (var materialUsePair in meshGameObjectPairs)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{materialUsePair.Key.name}");
                EditorGUILayout.LabelField($"{materialUsePair.Value.GetFullName()}");
                if (GUILayout.Button("Ping"))
                {
                    EditorGUIUtility.PingObject(materialUsePair.Value);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DisplayShaderUses(IEnumerable<KeyValuePair<Material, int>> materialUsePairs)
        {
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Material name", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Amount of uses", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            foreach (var materialUsePair in materialUsePairs)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{materialUsePair.Key.name}");
                EditorGUILayout.LabelField($"{materialUsePair.Value}");
                GUILayout.EndHorizontal();
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
            
            _materialUses.Add(material,
                (from gameObject in gameObjectsWithTargetMaterial
                    let mesh = gameObject.GetComponent<MeshFilter>().sharedMesh
                    select new KeyValuePair<Mesh, GameObject>(mesh, gameObject)).ToArray());
        }

        private void TakeMaterialSnapshot()
        {
            Camera mainCamera;
            if (Application.isPlaying)
            {
                if (CameraInfo.Instance == null)
                {
                    PLog.Error<HotspotLogger>(
                        "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
                    return;
                }

                mainCamera = CameraInfo.Instance.Main;

                if (mainCamera == null)
                {
                    PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                    return;
                }
            }
            else
            {
                mainCamera = SceneView.GetAllSceneCameras()[0];
            }
            
            
            // Get the renderers that are visible and in the frustum of the main camera
            _snapShotRenderers = _renderers.Where(x => x != null && x.isVisible).ToArray();
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