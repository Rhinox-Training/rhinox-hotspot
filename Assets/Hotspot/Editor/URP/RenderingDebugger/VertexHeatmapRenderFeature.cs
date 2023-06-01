using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatmapRenderFeature : ScriptableRendererFeature
    {
        private class VertexHeatmapPass : ScriptableRenderPass
        {
            private Material _material = null;
            private string _shaderName = "Hidden/Heatmap";
            
            private RenderTargetIdentifier _source;
            private int _tempRTId;
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                _source = renderingData.cameraData.renderer.cameraColorTarget;
            }
            

            public VertexHeatmapPass()
            {
                // Create a material with the given shader name.
                _material = CoreUtils.CreateEngineMaterial(_shaderName);
                
                // The render pass event should occur after all post-processing has been completed.
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                
                // Get the identifier for the temporary render target.
                _tempRTId = Shader.PropertyToID("_TempRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Vertex Heatmap Pass");
                
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

        private VertexHeatmapPass _pass;

        public override void Create()
        {
            _pass = new VertexHeatmapPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}