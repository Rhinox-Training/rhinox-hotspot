using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    // Class for setting up scene view rendering in the Unity editor
    [InitializeOnLoad]
    public class SceneViewRenderSetup
    {
        // Boolean to store the state of the heatmap feature
        private static bool _heatmapEnabled = false;
        private static VertexHeatmapRenderFeature _renderFeature = null;
        private static VertexHeatmapRenderFeature.VertexHeatmapSettings _heatmapSettings = null;
        private static Camera _mainCamera;
        private static Renderer[] _renderers;

        private static Texture2D _densityTexture;
        private static Color[] _pixels;

        static SceneViewRenderSetup()
        {
            CreateUI();
            
            _heatmapSettings = new VertexHeatmapRenderFeature.VertexHeatmapSettings();

            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Get the first vertex heatmap render feature
            _renderFeature = urpAsset.GetRenderFeature<VertexHeatmapRenderFeature>() as VertexHeatmapRenderFeature;
            if (_renderFeature)
            {
                _renderFeature.HeatmapSettings = _heatmapSettings;
            }

            SceneView.beforeSceneGui += CalculateVertexDensity;

            EditorSceneManager.sceneOpened += RefreshRenderers;
        }

        private static void CreateUI()
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

            var hexagonalUI = CreateHexagonalSettingsUI();
            var gaussUI = CreateGaussSettingsUI();
            
            widgetList.Add(box);
            widgetList.Add(heatmapCheckbox);
            widgetList.Add(hexagonalUI);
            widgetList.Add(gaussUI);

            // Create a new panel (tab) in the Rendering Debugger
            var panel = DebugManager.instance.GetPanel("Hotspot", createIfNull: true);

            // Add the widgets to the panel
            panel.children.Add(widgetList.ToArray());
        }

        private static DebugUI.Foldout CreateHexagonalSettingsUI()
        {
            var radiusUI = new DebugUI.UIntField()
            {
                displayName = "Hexagon blur radius",
                getter = () => _heatmapSettings.HexagonBlurRadius,
                setter = value => _heatmapSettings.HexagonBlurRadius = value,
                min = () => 1
            };

            var maxDensityUI = new DebugUI.UIntField()
            {
                displayName = "Max vertex density",
                getter = () => _heatmapSettings.MaxDensity,
                setter = value => _heatmapSettings.MaxDensity = value,
                min = () => 1
            };

            var hexagonalUI = new DebugUI.Foldout
            {
                displayName = "Hexagonal blur step settings",
                children =
                {
                    radiusUI,
                    maxDensityUI
                }
            };

            return hexagonalUI;
        }

        private static DebugUI.Foldout CreateGaussSettingsUI()
        {
            var radiusUI = new DebugUI.UIntField()
            {
                displayName = "Gaussian blur radius",
                getter = () => _heatmapSettings.GaussianBlurRadius,
                setter = value => _heatmapSettings.GaussianBlurRadius = value,
                min = () => 1
            };

            var sigmaUI = new DebugUI.FloatField()
            {
                displayName = "Gaussian blur sigma",
                getter = () => _heatmapSettings.GaussianBlurSigma,
                setter = value => _heatmapSettings.GaussianBlurSigma = value,
                min = () => 0.01f
            };
            var gaussUI = new DebugUI.Foldout
            {
                displayName = "Gaussian blur step settings",
                children =
                {
                    radiusUI,
                    sigmaUI
                }
            };

            return gaussUI;
        }
        
        private static void RefreshRenderers(Scene scene, OpenSceneMode mode)
        {
            _renderers = Object.FindObjectsOfType<Renderer>();
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
                if (_renderFeature != null) _renderFeature.HeatmapSettings = _heatmapSettings;
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
            }
        }

        private static void CalculateVertexDensity(SceneView sceneView)
        {
            if (!_heatmapEnabled)
                return;
            _renderFeature.HeatmapSettings = _heatmapSettings;
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
                _renderFeature.DensityTexture = _densityTexture;

                // Create an array with the size of all the pixels in the texture.
                _pixels = new Color[width * height];

                _densityTexture.SetPixels(_pixels);
            }

            // Reset all pixels to black and set all pixels in the texture
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = Color.black;

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