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

        private List<MaterialAnalysisInfo> _materialOccurrences = new();
        private List<ShaderAnalysisInfo> _shaderOccurrences = new();

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

            if (_shaderOccurrences != null)
            {
                RenderInfo(_materialOccurrences, "Occurrence of Materials:");
                RenderInfo(_shaderOccurrences, "Occurrence of Shaders:");
            }

            GUILayout.EndScrollView();
        }

        private void RenderInfo(IEnumerable<AnalysisInfo> occurrences, string paragraphTitle)
        {
            GUILayout.Space(5f);
            CustomEditorGUI.Title(paragraphTitle, EditorStyles.boldLabel);
            GUILayout.Space(10f);

            foreach (var item in occurrences)
            {
                item.OnGUI();
            }
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

            _materialOccurrences.Clear();
            _shaderOccurrences.Clear();

            // Get the renderers that are visible and in the frustum of the main camera
            _snapShotRenderers = _renderers.Where(x => x != null && x.isVisible).ToArray();
            _snapShotRenderers = _snapShotRenderers.Where(r => r.IsWithinFrustum(mainCamera)).ToArray();

            // From these renderers, get all the shared materials into one array.
            var materials = _snapShotRenderers.SelectMany(x => x.sharedMaterials).ToArray();
            materials = materials.Where(x => x != null).ToArray();
            
            // Find out all unique shaders and names used in these materials.
            var uniqueShaders = materials.Select(x => x.shader).Distinct().ToArray();
            var uniqueShaderNames = uniqueShaders.Select(x => x.name).Distinct().ToArray();

            // Update the count of total renderers, total materials and total unique shaders.
            _rendererCount = _snapShotRenderers.Length;
            _materialCount = materials.Length;
            _uniqueShaderCount = uniqueShaderNames.Length;

            // Group the materials by themselves to get the occurrence of each material.
            // Sort the material occurrence data in descending order.
            var materialPairs = materials
                .GroupBy(m => m)
                .Select(g => new KeyValuePair<Material, int>(g.Key, g.Count()));
            foreach (var pair in materialPairs)
                _materialOccurrences.Add(new MaterialAnalysisInfo(pair.Key, pair.Value, _snapShotRenderers, materials));
            _materialOccurrences.SortByDescending(x => x.AmountOfOccurrences);

            // Group the materials by shader name to get the occurrence of each shader.
            // Sort the shader occurrence data in descending order.
            var shaderPairs = materials.GroupBy(x => x.shader.name)
                .Select(x => new KeyValuePair<Shader, int>(x.First().shader, x.Count()));
            foreach (var pair in shaderPairs)
                _shaderOccurrences.Add(new ShaderAnalysisInfo(pair.Key, pair.Value, _snapShotRenderers));
            _shaderOccurrences.SortByDescending(x => x.AmountOfOccurrences);
        }
    }
}