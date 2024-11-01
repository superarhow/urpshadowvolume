using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace ZPStencilShadow
{

    public class ZPStencilShadowRenderPassFeature : ScriptableRendererFeature
    {

        private List<ZPStencilShadowObject> objects = new List<ZPStencilShadowObject>();

        internal class ZPStencilShadowRenderPass : ScriptableRenderPass
        {
            private ShaderTagId universalPassTag;
            private int frontPass;
            private int backPass;
            private int volumePass;
            private FilteringSettings filterSettings;
            private ZPStencilShadowRenderPassFeature feature;
            private RendererListDesc rendererListDesc;

            public ZPStencilShadowRenderPass(ZPStencilShadowRenderPassFeature feature, RenderPassEvent renderPassEvent)
            {
                this.feature = feature;
                this.renderPassEvent = renderPassEvent;

                universalPassTag = new ShaderTagId("UniversalForward");
                frontPass = feature.material?.FindPass("ShadowVolumeFrontFaces") ?? -1;
                backPass = feature.material?.FindPass("ShadowVolumeBackFaces") ?? -1;
                volumePass = feature.material?.FindPass("ShadowVolumeShadowPass") ?? -1;

                filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (feature?.material == null)
                {
                    Debug.LogError($"Please assign {nameof(ZPStencilShadowRenderPassFeature)}'s material");
                    return;
                }
                SetLightParameters(ref renderingData);

                if (feature.mode == EMode.Global)
                {
                    //Debug.Log(renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat);
                    // Create DrawingSettings based on the ShaderTagId
                    var drawingSettings = CreateDrawingSettings(universalPassTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                    drawingSettings.overrideMaterial = feature.material;
                    drawingSettings.overrideMaterialPassIndex = frontPass;
                    // Front face stencil op
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);

                    drawingSettings.overrideMaterialPassIndex = backPass;
                    // Back face stencil op
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);

                    drawingSettings.overrideMaterialPassIndex = volumePass;
                    // Shadow volume
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);
                }
                else
                {
                    var cmd = CommandBufferPool.Get(nameof(ZPStencilShadowRenderPass));
                    try
                    {
                        DrawRenderers(cmd, frontPass);
                        DrawRenderers(cmd, backPass);
                        DrawRenderers(cmd, volumePass);
                        context.ExecuteCommandBuffer(cmd);
                    }
                    finally
                    {
                        CommandBufferPool.Release(cmd);
                    }
                }
            }

            private void DrawRenderers(CommandBuffer cmd, int pass)
            {
                foreach (var renderer in feature.objects.SelectMany(x => x.Renderers))
                {
                    if (renderer == null) continue;
                    int n = renderer.sharedMaterials.Length;
                    for (int i = 0; i < n; i++)
                    {
                        cmd.DrawRenderer(renderer, feature.material, i, pass);
                    }
                }
            }

            private void SetLightParameters(ref RenderingData renderingData)
            {
                var c = renderingData.lightData.mainLightIndex;
                if (c >= 0 && c < renderingData.lightData.visibleLights.Length)
                {
                    var mainLight = renderingData.lightData.visibleLights[c];

                    // For Directional Light
                    if (mainLight.lightType == LightType.Directional)
                    {
                        Vector4 lightPos = mainLight.light.transform.forward; // Gets the direction
                        lightPos.w = 0.0f; // 0 indicates a directional light
                        Shader.SetGlobalVector("_WorldSpaceLightPos0", lightPos);

                        // Set color, intensity and other light properties as needed
                        Shader.SetGlobalColor("_LightColor0", mainLight.finalColor);
                    }
                    // Add handling for other types of lights if needed (point, spot, etc)
                }
                else
                {
                    // No main light, set to default
                    Shader.SetGlobalVector("_WorldSpaceLightPos0", Vector4.zero);
                    Shader.SetGlobalColor("_LightColor0", Color.black);
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }
        }

        private ZPStencilShadowRenderPass scriptablePass;

        public enum EMode
        {
            /// <summary>
            /// All opaque objects have shadow volumes
            /// </summary>
            Global = 0,
            /// <summary>
            /// The objects attached ZPStencilShadowObject script have shadow volumes
            /// </summary>
            Object
        }
        /// <summary>
        /// Something attached with shader "ZPStencilShadow/ShadowVolume"
        /// </summary>
        public Material material;
        public EMode mode = EMode.Global;
        /// <summary>
        /// When to inject our pass
        /// </summary>
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        public void RegisterObject(ZPStencilShadowObject obj)
        {
            objects.Add(obj);
        }

        public void UnregisterObject(ZPStencilShadowObject obj)
        {
            objects.Remove(obj);
        }

        /// <inheritdoc/>
        public override void Create()
        {
            scriptablePass = new ZPStencilShadowRenderPass(this, renderPassEvent);

            scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(scriptablePass);
        }
    }

}
