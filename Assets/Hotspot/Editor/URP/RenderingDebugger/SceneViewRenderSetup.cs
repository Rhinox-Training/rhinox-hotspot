using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
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

        private static Color[] _pixels;
        private static Texture2D _densityTexture;

        private const int GRADIENT_TEXTURE_WIDTH = 128;

        public static Texture2D GradientTexture
        {
            get
            {
                if (_gradientTexture == null)
                    _gradientTexture = CreateGradientTexture();

                return _gradientTexture;
            }
        }

        private static Texture2D _gradientTexture;

        static SceneViewRenderSetup()
        {
            CreateUI();

            _heatmapSettings = new VertexHeatmapRenderFeature.VertexHeatmapSettings
            {
                HeatmapTexture = GradientTexture
            };

            // Fetch the current render pipeline asset
            var urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Get the first vertex heatmap render feature
            _renderFeature = urpAsset.GetRenderFeature<VertexHeatmapRenderFeature>() as VertexHeatmapRenderFeature;
            if (_renderFeature)
            {
                _renderFeature.HeatmapSettings = _heatmapSettings;
            }

            EditorApplication.update -= SetRenderFeatureSettings;
            EditorApplication.update += SetRenderFeatureSettings;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorApplication.quitting += OnQuit;
        }

        private static void SetRenderFeatureSettings()
        {
            if(_renderFeature)
                _renderFeature.HeatmapSettings = _heatmapSettings;
        }

        private static void OnQuit()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorApplication.update -= SetRenderFeatureSettings;

            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Remove the vertex heatmap render feature
            urpAsset.RemoveAllRenderFeatures<VertexHeatmapRenderFeature>();
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
            panel.SetDirty();
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
                min = () => 1,
                max = () => 255
            };

            var textureUI = new TextureDebugUIField()
            {
                displayName = "Heatmap gradient texture",
                getter = () => _heatmapSettings.HeatmapTexture,
                setter = value => _heatmapSettings.HeatmapTexture = value
            };

            var hexagonalUI = new DebugUI.Foldout
            {
                displayName = "Hexagonal blur step settings",
                children =
                {
                    radiusUI,
                    maxDensityUI,
                    textureUI
                }
            };

            return hexagonalUI;
        }

        private static Texture2D CreateGradientTexture()
        {
            var gradientStops = new SortedDictionary<float, Color>
            {
                // Default gradient is sunrise
                // see https://learn.microsoft.com/en-us/bingmaps/v8-web-control/map-control-concepts/heat-map-module-examples/heat-map-color-gradients
                { 0f, Color.red },
                { .66f, Color.yellow },
                { 1f, Color.white }
            };

            return TextureFactory.Create1DGradientTexture(GRADIENT_TEXTURE_WIDTH, gradientStops);
        }

        private static DebugUI.Foldout CreateGaussSettingsUI()
        {
            var blurIterationsUI = new DebugUI.UIntField()
            {
                displayName = "Amount of blur iterations",
                getter = () => _heatmapSettings.AmountOfBlurIterations,
                setter = value => _heatmapSettings.AmountOfBlurIterations = value,
                min = () => 1
            };
            
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
                    blurIterationsUI,
                    radiusUI,
                    sigmaUI
                }
            };

            return gaussUI;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // Fetch the current render pipeline asset
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as
                UniversalRenderPipelineAsset;

            // Remove the vertex heatmap render feature
            urpAsset.RemoveAllRenderFeatures<VertexHeatmapRenderFeature>();

            _heatmapEnabled = false;
            _renderFeature.ReleaseRenderers();
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
            else
                _renderFeature = urpAsset.GetRenderFeature<VertexHeatmapRenderFeature>() as VertexHeatmapRenderFeature;

            // Enable or disable the heatmap feature based on the checkbox value
            if (toggleValue)
            {
                urpAsset.EnableRenderFeature<VertexHeatmapRenderFeature>();
                _renderFeature!.HeatmapSettings = _heatmapSettings;
            }
            else
            {
                urpAsset.DisableRenderFeature<VertexHeatmapRenderFeature>();
            }
        }
        
    }
}