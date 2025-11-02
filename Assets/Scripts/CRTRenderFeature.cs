using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class CRTRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private CRTRenderPass renderPass;

    public override void Create()
    {
        renderPass = new CRTRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            Debug.LogWarning("CRT Material is missing!");
            return;
        }

        renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderPass.SetTarget(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        renderPass?.Dispose();
    }

    class CRTRenderPass : ScriptableRenderPass
    {
        private Settings settings;
        private RTHandle source;
        private RTHandle tempTextureHandle;
        private const string k_TempTextureName = "_TempCRTTexture";

        public CRTRenderPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            this.source = colorHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTextureHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_TempTextureName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null || source == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("CRT Effect");

            // Blit from source to temp with material
            Blitter.BlitCameraTexture(cmd, source, tempTextureHandle, settings.material, 0);
            // Blit from temp back to source
            Blitter.BlitCameraTexture(cmd, tempTextureHandle, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTextureHandle?.Release();
        }
    }
}