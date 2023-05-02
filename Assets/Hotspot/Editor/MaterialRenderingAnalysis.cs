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
                var materials = renderers.SelectMany(x => x.sharedMaterials).ToArray();
                var shaders = materials.Select(x => x.shader.name).Distinct().ToArray();

                _rendererCount = renderers.Length;
                _materialCount = materials.Length;
                _uniqueShaderCount = shaders.Length;
            }
        }
    }
}