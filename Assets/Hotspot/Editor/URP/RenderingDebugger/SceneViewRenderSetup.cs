using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    // Class for setting up scene view rendering in the Unity editor
    [InitializeOnLoad]
    public class SceneViewRenderSetup
    {
        // Boolean to store the state of the heatmap feature
        private static bool _heatmapEnabled = false;
        private static VertexHeatmapRenderFeature _renderFeature = null;
        private static Camera _mainCamera;
        private static Renderer[] _renderers;

        private static Texture2D _densityTexture;
        private static Color[] _pixels;

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
                displayName =
                    "Disable Post processing in RenderingDebugger - Rendering - Post-processing, to ensure correct view.",
                style = DebugUI.MessageBox.Style.Warning
            };
            widgetList.Add(box);
            widgetList.Add(heatmapCheckbox);

            // Create a new panel (tab) in the Rendering Debugger
            var panel = DebugManager.instance.GetPanel("Hotspot", createIfNull: true);

            // Add the widgets to the panel
            panel.children.Add(widgetList.ToArray());

            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Get the first vertex heatmap render feature
            _renderFeature = urpAsset.GetRenderFeature<VertexHeatmapRenderFeature>() as VertexHeatmapRenderFeature;

            SceneView.beforeSceneGui += CalculateVertexDensity;
        }

        // Method to toggle the heatmap feature on and off
        private static void ToggleHeatmap(DebugUI.Field<bool> debugField, bool toggleValue)
        {
            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Check if the VertexHeatmapRenderFeature is registered, if not add it
            if (!urpAsset.HasRenderFeature<VertexHeatmapRenderFeature>())
            {
                _renderFeature = urpAsset.AddRenderFeature<VertexHeatmapRenderFeature>() as VertexHeatmapRenderFeature;
            }

            // Enable or disable the heatmap feature based on the checkbox value
            if (toggleValue)
            {
                urpAsset.EnableRenderFeature<VertexHeatmapRenderFeature>();
                CalculateVertexDensity(null);
            }
            else
            {
                urpAsset.DisableRenderFeature<VertexHeatmapRenderFeature>();
                _renderers = null;
                _pixels = null;
            }
        }

        private static void CalculateVertexDensity(SceneView sceneView)
        {
            if (!_heatmapEnabled)
                return;

            Profiler.BeginSample("CalculateVertexDensity");

            // Get Active camera
            if (_mainCamera == null)
                HotSpotUtils.TryGetMainCamera(out _mainCamera);

            // Get all renderers
            _renderers ??= Object.FindObjectsOfType<Renderer>();

            int width = _mainCamera.pixelWidth;
            int height = _mainCamera.pixelHeight;

            //Init the texture and white pixels
            if (_densityTexture == null || _densityTexture.width != width || _densityTexture.height != height)
            {
                _densityTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

                // Set the texture on the feature
                _renderFeature.DensityTexture2D = _densityTexture;

                // Create an array with the size of all the pixels in the texture.
                _pixels = new Color[width * height];
                for (int i = 0; i < _pixels.Length; i++)
                    _pixels[i] = Color.black;
            }

            // Set all pixels at once.
            _densityTexture.SetPixels(_pixels);

            // Loop over visible renderers
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null && renderer.isVisible && renderer.IsWithinFrustum(_mainCamera))
                {
                    // Create MeshInfo
                    var meshInfo = MeshInfo.Create(renderer);

                    // Loop over vertices
                    foreach (Vector3 meshVertex in meshInfo.Mesh.vertices)
                    {
                        // Transform the vertex to world space
                        var worldSpaceVertex = renderer.transform.TransformPoint(meshVertex);

                        //  Calculate screen space coordinates
                        var screenPos = _mainCamera.WorldToScreenPoint(worldSpaceVertex);

                        //  Filter out the ones outside view
                        if (screenPos.x.IsBetween(0, width) && screenPos.y.IsBetween(0, height))
                        {
                            // Get the previous color   
                            int index = (int)screenPos.y * width + (int)screenPos.x;
                            _pixels[index].r += 1;
                        }
                    }
                }
            }

            // Set all pixels at once.
            _densityTexture.SetPixels(_pixels);
            _densityTexture.Apply();

            Profiler.EndSample();
        }
    }
}