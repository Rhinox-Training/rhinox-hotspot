using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hotspot.Editor
{
    public class MaterialRenderingAnalysis : CustomEditorWindow
    {
        private Renderer[] _renderers;
        private int _rendererCount;
        private int _materialCount;
        private int _uniqueShaderCount;

        private Vector2 _scrollPos;
        //private string[] _uniqueShaders = null;             Occurrence
        private IEnumerable<KeyValuePair<string, int>> _shaderOccurrence = null;

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
            _renderers = GameObject.FindObjectsOfType<Renderer>();
        }

        protected override void OnGUI()
        {
            GUILayout.Label($"Renderers: {_rendererCount}");
            GUILayout.Label($"Materials: {_materialCount}");
            GUILayout.Label($"Unique Shaders: {_uniqueShaderCount}");

            if (GUILayout.Button("Take Material Snapshot"))
            {
                var renderers = _renderers.Where(x => x.isVisible).ToArray();
                //var renderers = _renderers.Where(x => x.isPartOfStaticBatch).ToArray();
                var materials = renderers.SelectMany(x => x.sharedMaterials).ToArray();
                var shaders = materials.Select(x => x.shader.name).Distinct().ToArray();

                _rendererCount = renderers.Length;
                _materialCount = materials.Length;
                _uniqueShaderCount = shaders.Length;

                _shaderOccurrence = materials.GroupBy(x => x.shader.name).Select(x => new KeyValuePair<string, int>(x.Key, x.Count()));
            }


            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(50f));
            foreach (var item in _shaderOccurrence)
            {
                GUILayout.Label($"{item.Key}: {item.Value}");
            }
            GUILayout.EndScrollView();
        }
    }
}