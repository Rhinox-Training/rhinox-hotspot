using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatmapRenderFeature : ScriptableRendererFeature
    {
        public class VertexHeatmapSettings
        {
            public uint MaxDensity = 25;
            public uint HexagonBlurRadius = 5;
            public uint GaussianBlurRadius = 5;
            public float GaussianBlurSigma = 1f;
            public uint AmountOfBlurIterations = 2;

            public Texture2D HeatmapTexture;
        }

        private class VertexHeatmapPass : ScriptableRenderPass
        {
            private VertexHeatmapSettings _heatmapSettings;
            private Material _material = null;
            private string _hexagonShaderName = "Hidden/HeatHexagonBlur";

            private Material _gaussMaterial = null;
            private string _gaussBlurShaderName = "Hidden/GaussianBlur";

            private RenderTargetIdentifier _source;
            private int _tempRTId;

            private static readonly int DensityTexId = Shader.PropertyToID("_DensityTex");
            private static readonly int MaxDensityId = Shader.PropertyToID("_MaxDensity");
            private static readonly int RadiusId = Shader.PropertyToID("_Radius");
            private static readonly int Sigma = Shader.PropertyToID("_Sigma");
            private static readonly int HeatTex = Shader.PropertyToID("_HeatTex");

            private Texture2D _densityTexture;

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

        public Texture2D DensityTexture = null;
        private VertexHeatmapPass _heatmapPass;
        public VertexHeatmapSettings HeatmapSettings;

        public override void Create()
        {
            _heatmapPass = new VertexHeatmapPass();
            HeatmapSettings = new VertexHeatmapSettings();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Pass the array to the shader
            _heatmapPass.SetHeatmapSettings(HeatmapSettings);
            _heatmapPass.SetDensityTexture(DensityTexture);
            renderer.EnqueuePass(_heatmapPass);
        }
    }
}