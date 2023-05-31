using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    public class VertexHeatmapRenderFeature : ScriptableRendererFeature
    {
        private class VertexHeatmapPass : ScriptableRenderPass
        {
            private RenderTargetIdentifier Source { get; set; }
            private Material _heatmapMaterial = null;
            private RenderTargetHandle _temporaryColorTexture;

            public void Setup(RenderTargetIdentifier source)
            {
                this.Source = source;
            }

            public VertexHeatmapPass()
            {
                _heatmapMaterial = new Material(Shader.Find("Custom/Heatmap"));
                _temporaryColorTexture.Init("_TemporaryColorTexture");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("VertexHeatmapRenderFeature");

                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                // Setup RenderTexture that will store the color inversion result.
                cmd.GetTemporaryRT(_temporaryColorTexture.id, opaqueDesc, FilterMode.Bilinear);

                Blit(cmd, Source, _temporaryColorTexture.Identifier(), _heatmapMaterial);

                // Now blit into the source
                Blit(cmd, _temporaryColorTexture.Identifier(), Source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (_temporaryColorTexture != RenderTargetHandle.CameraTarget)
                {
                    cmd.ReleaseTemporaryRT(_temporaryColorTexture.id);
                    _temporaryColorTexture = RenderTargetHandle.CameraTarget;
                }
            }
        }

        private VertexHeatmapPass _pass;

        public override void Create()
        {
            _pass = new VertexHeatmapPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _pass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(_pass);
        }
    }
}