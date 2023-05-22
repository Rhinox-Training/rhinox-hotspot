using Rhinox.Magnus;
using Rhinox.Perceptor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hotspot
{
    public static class HotSpotUtils
    {
        public static bool TryGetMainCamera(out Camera camera)
        {
            camera = null;

            if (Application.isPlaying)
            {
                if (CameraInfo.Instance == null)
                {
                    PLog.Warn<HotspotLogger>(
                        "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
                    return false;
                }

                camera = CameraInfo.Instance.Main;
            }
#if UNITY_EDITOR
            else
            {
                camera = SceneView.lastActiveSceneView.camera;
            }
#endif
            if (camera == null)
            {
                PLog.Warn<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                return false;
            }

            return true;
        }
    }
}