using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    // Class for setting up scene view rendering in the Unity editor
    [InitializeOnLoad]
    public class SceneViewRenderSetup
    {
        // Boolean to store the state of the heatmap feature
        private static bool _heatmapEnabled = true;

        static SceneViewRenderSetup()
        {
            // Create a list of widgets for the DebugUI
            var widgetList = new List<DebugUI.Widget>();

            // Create and add a checkbox widget for enabling/disabling the heatmap feature
            var heatmapCheckbox = new DebugUI.BoolField
            {
                displayName = "Enable vertex density view",
                tooltip = "Generates a heatmap overlay on the scene view",
                getter = () => _heatmapEnabled,
                setter = value => _heatmapEnabled = value,
                onValueChanged = ToggleHeatmap
            };
            var box = new DebugUI.MessageBox
            {
                displayName = "Disable Post processing in RenderingDebugger - Rendering - Post-processing, to ensure correct view.",
                style = DebugUI.MessageBox.Style.Warning
            };
            widgetList.Add(box);
            widgetList.Add(heatmapCheckbox);

            // Create a new panel (tab) in the Rendering Debugger
            var panel = DebugManager.instance.GetPanel("Hotspot", createIfNull: true);

            // Add the widgets to the panel
            panel.children.Add(widgetList.ToArray());
        }

        // Method to toggle the heatmap feature on and off
        private static void ToggleHeatmap(DebugUI.Field<bool> debugField, bool toggleValue)
        {
            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Check if the VertexHeatmapRenderFeature is registered, if not add it
            if (!urpAsset.HasRenderFeature<VertexHeatmapRenderFeature>())
                urpAsset.AddRenderFeature<VertexHeatmapRenderFeature>();

            // Enable or disable the heatmap feature based on the checkbox value
            if (toggleValue)
                urpAsset.EnableRenderFeature<VertexHeatmapRenderFeature>();
            else
                urpAsset.DisableRenderFeature<VertexHeatmapRenderFeature>();
        }
    }
}