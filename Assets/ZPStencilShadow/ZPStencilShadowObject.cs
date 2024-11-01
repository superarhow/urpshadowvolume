using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZPStencilShadow
{
    [ExecuteAlways]
    public class ZPStencilShadowObject : MonoBehaviour
    {
        public Renderer[] Renderers;
        private ZPStencilShadowRenderPassFeature renderFeature;

        private void OnEnable()
        {
            renderFeature = GetRenderFeature<ZPStencilShadowRenderPassFeature>();
            if (renderFeature == null) return;
            renderFeature.RegisterObject(this);
        }

        private void OnDisable()
        {
            renderFeature?.UnregisterObject(this);
        }

        private void Reset()
        {
            Renderers = GetComponentsInChildren<Renderer>(true);
        }

        private T GetRenderFeature<T>() where T : ScriptableRendererFeature
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipelineAsset == null)
            {
                Debug.LogError("Current render pipeline is not UniversalRenderPipelineAsset");
                return null;
            }

            // Get the current renderer
            var renderer = pipelineAsset.scriptableRenderer;
            if (renderer == null)
            {
                Debug.LogError("Failed to get ScriptableRenderer from UniversalRenderPipelineAsset");
                return null;
            }

            // Use reflection to access the protected rendererFeatures property
            System.Type rendererType = renderer.GetType();
            PropertyInfo featuresProp = rendererType.GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);

            if (featuresProp == null)
            {
                Debug.LogError("Failed to find rendererFeatures field");
                return null;
            }

            var rendererFeatures = featuresProp.GetValue(renderer) as List<ScriptableRendererFeature>;
            if (rendererFeatures == null)
            {
                Debug.LogError("Failed to get rendererFeatures");
                return null;
            }

            // Iterate through all ScriptableRendererFeatures and find the one you need
            foreach (var feature in rendererFeatures)
            {
                // Check if the feature is the one you are looking for
                // For example, let's say you are looking for a custom feature named "MyCustomFeature"
                if (feature is T)
                {
                    // Do something with the feature
                    var myFeature = feature as T;
                    Debug.Log("Found Feature: " + myFeature.name);
                    return myFeature;
                }
            }
            return null;
        }
    }
}
