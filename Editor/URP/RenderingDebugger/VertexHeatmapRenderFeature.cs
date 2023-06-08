using System.Linq;
using UnityEngine;
using Rhinox.Lightspeed;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatmapRenderFeature : ScriptableRendererFeature
    {
        // Class representing settings for vertex heatmap shader
        public class VertexHeatmapSettings
        {
            public uint MaxDensity = 25;
            public uint HexagonBlurRadius = 5;
            public uint GaussianBlurRadius = 5;
            public float GaussianBlurSigma = 1f;
            public uint AmountOfBlurIterations = 2;

            public Texture2D HeatmapTexture
            {
                get => _heatmapTexture;
                set
                {
                    if (_heatmapTexture != value)
                    {
                        if (value != null)
                        {
                            value.wrapMode = TextureWrapMode.Mirror;
                            value.filterMode = FilterMode.Point;
                        }
                        _heatmapTexture = value;
                    }
                }
            }
            private Texture2D _heatmapTexture;
        }

        // Class representing a render pass for vertex heatmap
        private class VertexHeatmapPass : ScriptableRenderPass
        {
            // Shader and Material related private fields
            private Material _material = null;
            private Material _gaussMaterial = null;
            private string _hexagonShaderName = "Hidden/HeatHexagonBlur";
            private string _gaussBlurShaderName = "Hidden/GaussianBlur";

            // Shader property IDs
            private static readonly int DensityTexId = Shader.PropertyToID("_DensityTex");
            private static readonly int MaxDensityId = Shader.PropertyToID("_MaxDensity");
            private static readonly int RadiusId = Shader.PropertyToID("_Radius");
            private static readonly int Sigma = Shader.PropertyToID("_Sigma");
            private static readonly int HeatTex = Shader.PropertyToID("_HeatTex");

            // Texture-related private fields
            private int _tempRTId;
            private Texture2D _densityTexture;
            private RenderTargetIdentifier _source;
            private VertexHeatmapSettings _heatmapSettings;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                _source = renderingData.cameraData.renderer.cameraColorTarget;
            }


            public VertexHeatmapPass()
            {
                // Create a material with the given shader name.
                _material = CoreUtils.CreateEngineMaterial(_hexagonShaderName);
                _gaussMaterial = CoreUtils.CreateEngineMaterial(_gaussBlurShaderName);

                // The render pass event should occur after all post-processing has been completed.
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

                // Get the identifier for the temporary render target.
                _tempRTId = Shader.PropertyToID("_TempRT");

                _heatmapSettings = new VertexHeatmapSettings();
            }

            // Make sure that the length of the density array matches the amount of pixels on the screen
            public void SetHeatmapSettings(VertexHeatmapSettings settings)
            {
                _heatmapSettings = settings;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Vertex Heatmap Pass");

                // Set the shader properties
                _material.SetTexture(DensityTexId, _densityTexture);
                _material.SetInt(MaxDensityId, (int)_heatmapSettings.MaxDensity);
                _material.SetInt(RadiusId, (int)_heatmapSettings.HexagonBlurRadius);
                _gaussMaterial.SetInt(RadiusId, (int)_heatmapSettings.GaussianBlurRadius);
                _gaussMaterial.SetFloat(Sigma, _heatmapSettings.GaussianBlurSigma);
                _material.SetTexture(HeatTex, _heatmapSettings.HeatmapTexture);

                // Create temporary RenderTexture
                cmd.GetTemporaryRT(_tempRTId, renderingData.cameraData.cameraTargetDescriptor);

                // Copy the source render texture to the temporary render texture.
                cmd.Blit(_source, _tempRTId);

                // Apply the material to the temporary render texture and write the output to the source.
                cmd.Blit(_tempRTId, _source, _material);

                // Apply the Gaussian blur as often as desired (at least 1)
                for (uint i = 0; i < _heatmapSettings.AmountOfBlurIterations; i++)
                {
                    // Apply horizontal Gaussian blur
                    cmd.Blit(_source, _tempRTId, _gaussMaterial, 0);
                    // Apply vertical Gaussian blur
                    cmd.Blit(_tempRTId, _source, _gaussMaterial, 1);
                }


                // Execute the command buffer and release it
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // Release temporary RT
                cmd.ReleaseTemporaryRT(_tempRTId);
            }

            public void SetDensityTexture(Texture2D densityTexture)
            {
                _densityTexture = densityTexture;
            }
        }

        private Texture2D _densityTexture = null;
        public VertexHeatmapSettings HeatmapSettings;

        private VertexHeatmapPass _heatmapPass;
        private Renderer[] _renderers;
        private Color[] _pixels;

        public override void Create()
        {
            _heatmapPass = new VertexHeatmapPass();
            HeatmapSettings = new VertexHeatmapSettings();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            CalculateVertexDensity(renderingData.cameraData.camera);

            //Pass the array to the shader
            _heatmapPass.SetHeatmapSettings(HeatmapSettings);
            _heatmapPass.SetDensityTexture(_densityTexture);
            renderer.EnqueuePass(_heatmapPass);
        }

        private void CalculateVertexDensity(Camera cam)
        {
            Profiler.BeginSample("CalculateVertexDensity");

            // Get Active camera
            if (cam == null)
                HotSpotUtils.TryGetMainCamera(out cam);

            // Get all renderers
            _renderers ??= FindObjectsOfType<Renderer>().Where(x => x != null).ToArray();

            int width = cam.pixelWidth;
            int height = cam.pixelHeight;

            Profiler.BeginSample("Texture setup");
            Profiler.BeginSample("Init texture");
            //Init the texture and white pixels
            if (_densityTexture == null || _densityTexture.width != width || _densityTexture.height != height)
            {
                _densityTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                // Create an array with the size of all the pixels in the texture.
                _pixels = new Color[width * height];

                _densityTexture.SetPixels(_pixels);
            }

            Profiler.EndSample();
            Profiler.BeginSample("Reset pixels");


            // Reset all pixels to black and set all pixels in the texture
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = Color.black;
            Profiler.EndSample();
            Profiler.EndSample();

            // Init the pixel step
            float pixelStep = 1f / HeatmapSettings.MaxDensity;

            Profiler.BeginSample("Vertex density loop");

            // Loop over visible renderers
            foreach (Renderer renderer in _renderers)
            {
                Profiler.BeginSample("Render evaluation");

                if (renderer.isVisible && renderer.IsWithinFrustum(cam))
                {
                    Profiler.BeginSample("Mesh evaluation");

                    // Create MeshInfo
                    var meshInfo = MeshInfo.Create(renderer);

                    Profiler.BeginSample("Vertex evaluation loop");

                    // Loop over vertices
                    foreach (Vector3 meshVertex in meshInfo.Mesh.vertices)
                    {
                        // Transform the vertex to world space
                        var worldSpaceVertex = renderer.transform.TransformPoint(meshVertex);

                        //  Calculate screen space coordinates
                        var screenPos = cam.WorldToScreenPoint(worldSpaceVertex,Camera.MonoOrStereoscopicEye.Mono);

                        //  Filter out the ones outside view
                        if (screenPos.x.IsBetween(0, width) && screenPos.y.IsBetween(0, height))
                        {
                            // Get the previous color   
                            int index = (int)screenPos.y * width + (int)screenPos.x;
                            _pixels[index].r += pixelStep;
                        }
                    }

                    Profiler.EndSample();
                    Profiler.EndSample();
                }

                Profiler.EndSample();
            }

            Profiler.EndSample();

            Profiler.BeginSample("Set pixels");
            // Set all pixels at once.
            _densityTexture.SetPixels(_pixels);
            _densityTexture.Apply();
            Profiler.EndSample();
            Profiler.EndSample();
        }

        public void ReleaseRenderers()
        {
            _renderers = null;
        }
    }
}