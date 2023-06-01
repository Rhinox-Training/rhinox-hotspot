using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hotspot.Editor
{
    /// <summary>
    /// Extension methods for Universal Render Pipeline asset.
    /// </summary>
    public static class URPAssetExtensions
    {
        private static readonly FieldInfo RendererDataListField;

        static URPAssetExtensions()
        {
            var type = typeof(UniversalRenderPipelineAsset);
            RendererDataListField = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Gets the array of ScriptableRendererData from the Universal Render Pipeline asset.
        /// </summary>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        /// <returns>The array of ScriptableRendererData.</returns>
        private static ScriptableRendererData[] GetRendererDataArray(UniversalRenderPipelineAsset asset)
        {
            return RendererDataListField?.GetValue(asset) as ScriptableRendererData[];
        }

        /// <summary>
        /// Finds the first renderer feature of the specified type within the given array of ScriptableRendererData.
        /// </summary>
        /// <typeparam name="T">The type of the renderer feature to find.</typeparam>
        /// <param name="scriptableRenderData">The array of ScriptableRendererData to search.</param>
        /// <returns>The found renderer feature or null if not found.</returns>
        private static ScriptableRendererFeature FindRendererFeature<T>(ScriptableRendererData[] scriptableRenderData)
            where T : ScriptableRendererFeature
        {
            return scriptableRenderData
                .SelectMany(data => data.rendererFeatures)
                .FirstOrDefault(feature => feature is T);
        }

        /// <summary>
        /// Disables the renderer feature of the specified type in the Universal Render Pipeline asset.
        /// </summary>
        /// <typeparam name="T">The type of the renderer feature to disable.</typeparam>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        /// <returns>The disabled renderer feature or null if not found.</returns>
        public static ScriptableRendererFeature DisableRenderFeature<T>(this UniversalRenderPipelineAsset asset)
            where T : ScriptableRendererFeature
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                var rendererFeature = FindRendererFeature<T>(scriptableRenderData);
                if (rendererFeature != null)
                {
                    rendererFeature.SetActive(false);
                    scriptableRenderData[0].SetDirty();
                    return rendererFeature;
                }
            }

            return null;
        }

        /// <summary>
        /// Enables the renderer feature of the specified type in the Universal Render Pipeline asset.
        /// </summary>
        /// <typeparam name="T">The type of the renderer feature to enable.</typeparam>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        /// <returns>The enabled renderer feature or null if not found.</returns>
        public static ScriptableRendererFeature EnableRenderFeature<T>(this UniversalRenderPipelineAsset asset)
            where T : ScriptableRendererFeature
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                var rendererFeature = FindRendererFeature<T>(scriptableRenderData);
                if (rendererFeature != null)
                {
                    rendererFeature.SetActive(true);
                    scriptableRenderData[0].SetDirty();
                    return rendererFeature;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a new renderer feature of the specified type to the Universal Render Pipeline asset.
        /// </summary>
        /// <typeparam name="T">The type of the renderer feature to add.</typeparam>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        /// <param name="renderDataIndex">The index of the renderer data to add the feature to (optional).</param>
        /// <returns>The added renderer feature or null if failed.</returns>
        public static ScriptableRendererFeature AddRenderFeature<T>(this UniversalRenderPipelineAsset asset,
            int renderDataIndex = 0)
            where T : ScriptableRendererFeature, new()
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                renderDataIndex = Mathf.Clamp(renderDataIndex, 0, scriptableRenderData.Length - 1);
                var renderData = scriptableRenderData[renderDataIndex];
                var rendererFeature = ScriptableObject.CreateInstance<T>();
                renderData.rendererFeatures.Add(rendererFeature);
                rendererFeature.Create();
                renderData.SetDirty();
                return rendererFeature;
            }

            return null;
        }

        /// <summary>
        /// Removes the renderer feature of the specified type from the Universal Render Pipeline asset.
        /// </summary>
        /// <typeparam name="T">The type of the renderer feature to remove.</typeparam>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        /// <returns>The removed renderer feature or null if not found.</returns>
        public static ScriptableRendererFeature RemoveRenderFeature<T>(this UniversalRenderPipelineAsset asset)
            where T : ScriptableRendererFeature, new()
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                foreach (var renderData in scriptableRenderData)
                {
                    var rendererFeature = renderData.rendererFeatures.FirstOrDefault(f => f is T);
                    if (rendererFeature != null)
                    {
                        rendererFeature.SetActive(true);
                        renderData.rendererFeatures.Remove(rendererFeature);
                        renderData.SetDirty();
                        return rendererFeature;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Removes all renderer features of the specified type from the Universal Render Pipeline asset.
        /// </summary>
        /// <typeparam name="T">The type of the renderer features to remove.</typeparam>
        /// <param name="asset">The Universal Render Pipeline asset.</param>
        public static void RemoveAllRenderFeatures<T>(this UniversalRenderPipelineAsset asset)
            where T : ScriptableRendererFeature, new()
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                foreach (var renderData in scriptableRenderData)
                {
                    renderData.rendererFeatures.RemoveAll(feature => feature is T);
                    renderData.SetDirty();
                }
            }
        }

        public static bool HasRenderFeature<T>(this UniversalRenderPipelineAsset asset)
            where T : ScriptableRendererFeature, new()
        {
            var scriptableRenderData = GetRendererDataArray(asset);
            if (scriptableRenderData != null)
            {
                return scriptableRenderData.Any(renderData =>
                    renderData.rendererFeatures.Find(feature => feature is T));
            }

            return false;
        }

        public static bool SetPostProcessDataOnRenderer(this UniversalRenderPipelineAsset asset, PostProcessData data, int renderDataIndex = 0)
        {
            var scriptableRenderData = GetRendererDataArray(asset);

            if (renderDataIndex >= scriptableRenderData.Length || renderDataIndex < 0)
                return false;
            
            var renderData = scriptableRenderData[renderDataIndex] as UniversalRendererData;
            if (renderData == null)
                return false;
            renderData.postProcessData = data;
            renderData.SetDirty();
            
            return true;
        }
    }
}