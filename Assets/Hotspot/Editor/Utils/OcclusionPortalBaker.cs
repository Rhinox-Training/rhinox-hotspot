using Hotspot.Utils;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor.Utils
{
    public static class OcclusionPortalBaker
    {
        [InitializeOnLoadMethod]
        private static void InitHooks()
        {
            OcclusionBakeDetector.BakeStarted -= OnBakeStarted;
            OcclusionBakeDetector.BakeStarted += OnBakeStarted;
        }

        private static void OnBakeStarted()
        {
            var portals = GameObject.FindObjectsOfType<OcclusionPortal>();
            foreach (var portal in portals)
            {
                if (portal == null)
                    continue;

                OcclusionPortalBakeData.CreateOrUpdate(portal);
            }
        }
    }
}