using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatMapWindow : CustomEditorWindow
    {
        private static VertexHeatmapRenderFeature _heatmapFeature;
        private bool _featureAdded;

        [MenuItem(HotspotWindowHelper.ANALYSIS_PREFIX + "Vertex Heatmap", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(VertexHeatMapWindow));
            win.titleContent = new GUIContent("Vertex Heatmap");
        }

        protected override void OnEndDrawEditors()
        {
            GUILayout.Label("Vertex Heatmap", EditorStyles.boldLabel);
            bool temp = GUILayout.Toggle(_featureAdded, "Show Heatmap");
            if (temp != _featureAdded)
            {
                _featureAdded = temp;
                ToggleHeatmap(_featureAdded);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(!_heatmapFeature)
                return;
            
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;
            urpAsset.RemoveAllRenderFeatures<VertexHeatmapRenderFeature>();
            DestroyImmediate(_heatmapFeature);
        }

        private void ToggleHeatmap(bool val)
        {
            // Register render feature if needed
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            if (urpAsset == null)
                return;
            if (!_heatmapFeature)
            {
                _heatmapFeature = (VertexHeatmapRenderFeature)urpAsset.AddRenderFeature<VertexHeatmapRenderFeature>();
            }

            if (val)
                urpAsset.EnableRenderFeature<VertexHeatmapRenderFeature>();
            else
                urpAsset.DisableRenderFeature<VertexHeatmapRenderFeature>();
        }
    }
}