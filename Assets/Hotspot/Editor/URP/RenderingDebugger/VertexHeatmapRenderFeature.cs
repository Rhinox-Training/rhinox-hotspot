using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatmapRenderFeature : ScriptableRendererFeature
    {
        private class VertexHeatmapPass : ScriptableRenderPass
        {
            public uint MaxDensity = 5;
            private Material _material = null;
            private string _hexagonShaderName = "Hidden/HeatHexagonBlur";
            
            private RenderTargetIdentifier _source;
            private int _tempRTId;
            
            private Texture2D _densityTexture;
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                _source = renderingData.cameraData.renderer.cameraColorTarget;
            }
            

            public VertexHeatmapPass()
            {
                // Create a material with the given shader name.
                _material = CoreUtils.CreateEngineMaterial(_hexagonShaderName);
                
                // The render pass event should occur after all post-processing has been completed.
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                
                // Get the identifier for the temporary render target.
                _tempRTId = Shader.PropertyToID("_TempRT");
            }

            // Make sure that the length of the density array matches the amount of pixels on the screen
            public void SetShaderTexture(Texture2D densityTexture)
            {
                _densityTexture = densityTexture;
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Vertex Heatmap Pass");

                // Set the shader properties
                _material.SetTexture(DensityTexId, _densityTexture);
                _material.SetInt(MaxDensityId, (int)MaxDensity);
                
                // Create temporary RenderTexture
                cmd.GetTemporaryRT(_tempRTId, renderingData.cameraData.cameraTargetDescriptor);

                // Copy the source render texture to the temporary render texture.
                cmd.Blit(_source, _tempRTId);

                // Apply the material to the temporary render texture and write the output to the source.
                cmd.Blit(_tempRTId, _source, _material);
                
                // Execute the command buffer and release it
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // Release temporary RT
                cmd.ReleaseTemporaryRT(_tempRTId);
            }
        }

        private VertexHeatmapPass _heatmapPass;
        public Texture2D DensityTexture2D;
        public uint MaxDensity = 5;
        
        private static readonly int DensityTexId = Shader.PropertyToID("_DensityTex");
        private static readonly int MaxDensityId = Shader.PropertyToID("_MaxDensity");

        public override void Create()
        {
            _heatmapPass = new VertexHeatmapPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Pass the array to the shader
            _heatmapPass.SetShaderTexture(DensityTexture2D);
            _heatmapPass.MaxDensity = MaxDensity;
            renderer.EnqueuePass(_heatmapPass);
        }
    }
}