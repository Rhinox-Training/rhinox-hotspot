using System.Collections;
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

        private IEnumerable<KeyValuePair<Shader, int>> _shaderOccurrence = null;

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
                var uniqueShaders = materials.Select(x => x.shader).Distinct().ToArray();
                var uniqueShaderNames = uniqueShaders.Select(x => x.name).Distinct().ToArray();

                _rendererCount = renderers.Length;
                _materialCount = materials.Length;
                _uniqueShaderCount = uniqueShaderNames.Length;

                _shaderOccurrence = materials.GroupBy(x => x.shader.name).Select(x => new KeyValuePair<Shader, int>(x.First().shader, x.Count()));
                _shaderOccurrence.OrderByDescending(x => x.Value);
            }

            if (_shaderOccurrence != null)
            {
                GUILayout.Space(5f);
                GUILayout.Label($"Occurrence of Shaders:");
                GUILayout.Space(10f);

                foreach (var shader in _shaderOccurrence)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{shader.Key.name}: {shader.Value}");
                    using (new eUtility.DisabledGroup())
                    {
                        EditorGUILayout.ObjectField(shader.Key, typeof(UnityEngine.Object), false);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}