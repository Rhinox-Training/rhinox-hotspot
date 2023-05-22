using Rhinox.Magnus;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

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
            else
            {
                camera = SceneView.lastActiveSceneView.camera;
            }

            if (camera == null)
            {
                PLog.Warn<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                return false;
            }
            return true;
        }
    }
}