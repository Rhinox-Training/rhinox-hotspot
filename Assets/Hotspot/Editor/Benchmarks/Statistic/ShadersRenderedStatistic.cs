using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Editor
{
    public enum ShaderRendererFilter
    {
        Instances,
        ByShader,
        ByShaderVariant
    }
    
    [Serializable]
    public class ShadersRenderedStatistic : ViewedObjectBenchmarkStatistic
    {
        public ShaderRendererFilter Filter = ShaderRendererFilter.Instances;
        
        private Shader[] _shaders;

        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            var materials = visibleRenderers.SelectMany(x => x.sharedMaterials).Where(x => x != null);

            switch (Filter)
            {
                case ShaderRendererFilter.Instances:
                    _shaders = materials.Select(x => x.shader).ToArray();
                    break;
                case ShaderRendererFilter.ByShader:
                    _shaders = materials.Select(x => x.shader).Distinct().ToArray();
                    break;
                case ShaderRendererFilter.ByShaderVariant:
                    _shaders = GetVariants(materials).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerable<Shader> GetVariants(IEnumerable<Material> materials)
        {
            return materials.Where(x => x.shader != null).DistinctBy(x => x.shader.GetHashCode() ^ GetHashCode(x.shaderKeywords)).Select(x => x.shader);
        }

        private static int GetHashCode(string[] shaderKeywords)
        {
            if (shaderKeywords.IsNullOrEmpty())
                return 0;
            int hashCode = shaderKeywords.First().GetHashCode();
            for (int index = 1; index < shaderKeywords.Length; ++index)
            {
                var keyword = shaderKeywords[index];
                hashCode ^= keyword.GetHashCode();
            }
            return hashCode;
        }

        protected override float SampleStatistic()
        {
            return (_shaders != null ? _shaders.Length : 0);
        }

        protected override string GetStatNameInner()
        {
            switch (Filter)
            {
                case ShaderRendererFilter.Instances:
                    return "Shaders Rendered (Instances)";
                case ShaderRendererFilter.ByShader:
                    return "Shaders Rendered (Types)";
                case ShaderRendererFilter.ByShaderVariant:
                    return "Shaders Rendered (Variants)";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}