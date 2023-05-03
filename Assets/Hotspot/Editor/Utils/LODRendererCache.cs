using System.Collections.Generic;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class LODRendererCache
    {
        private class LODInfo
        {
            public LODGroup Group;
            public int LODLevel;

            public bool IsLOD()
            {
                return LODLevel > 0;
            }
        }

        private static Dictionary<Renderer, LODInfo> _rendererCache;

        public static bool IsLOD(Renderer renderer)
        {
            if (_rendererCache == null)
                _rendererCache = new Dictionary<Renderer, LODInfo>();

            if (_rendererCache.ContainsKey(renderer))
            {
                var lodInfo = _rendererCache[renderer];
                return lodInfo != null && lodInfo.IsLOD();
            }

            var lodGroup = renderer.GetComponentInParent<LODGroup>();
            LODInfo info = null;
            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                for (int i = 0; i < lods.Length; ++i)
                {
                    var lod = lods[i];
                    foreach (var lodRenderer in lod.renderers)
                    {
                        if (lodRenderer == null)
                            continue;

                        if (lodRenderer == renderer)
                        {
                            info = new LODInfo()
                            {
                                Group = lodGroup,
                                LODLevel = i
                            };
                        }
                        else
                        {
                            _rendererCache.Add(lodRenderer, new LODInfo()
                            {
                                Group = lodGroup,
                                LODLevel = i
                            });
                        }
                    }
                }
            }
            _rendererCache.Add(renderer, info);

            return info != null && info.IsLOD();
        }
    }
}