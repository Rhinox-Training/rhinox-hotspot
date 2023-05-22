using System.Collections.Generic;
using System.Linq;
using Hotspot.Utils;
using Rhinox.Lightspeed;
using Rhinox.Magnus;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

namespace Hotspot
{
    [ExecuteInEditMode]
    public class LODGhostView : MonoBehaviour
    {
        public LODGroup LODGroup { get; set; }
        public Camera Cam;


        private int _currentLOD = 0;
        private int _currentLODPercentage = 0;
        
        private void Update()
        {
            if (Cam == null || LODGroup == null)
            {
                Initialize();
                return;
            }

            float relativeHeight = CalculateLODPercentage();
            Debug.Log(relativeHeight.ToString("###"));
        }

        private float CalculateLODPercentage()
        {
            // Calculates the distance between LODGroup and the camera
            float distance = Vector3.Distance(LODGroup.transform.position, Cam.transform.position);
            if (distance == 0f)
            {
                PLog.Error<HotspotLogger>(
                    "Distance between LODGroup and Camera is zero, can't proceed as it will lead to division by zero.");
                return -1;
            }

            // Computes the height based on the size of the LODGroup and the multiplier
            float height = LODGroup.size;
            
            // Calculates the relative height based on the reciprocal of the distance times the height
            // Also check the lodBias in the QualitySettings
            return 100f / distance * height * QualitySettings.lodBias;
        }

        private void Initialize()
        {
            TryGetMainCamera(out Cam);
        }

        private bool TryGetMainCamera(out Camera cam)
        {
            cam = null;

            if (Application.isPlaying)
            {
                if (CameraInfo.Instance == null)
                {
                    PLog.Warn<HotspotLogger>(
                        "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
                    return false;
                }

                cam = CameraInfo.Instance.Main;
            }
            else
                cam = Camera.main;

            if (cam == null)
            {
                PLog.Warn<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                return false;
            }

            var cams = SceneView.GetAllSceneCameras();
            cam = SceneView.GetAllSceneCameras()[0];
            cam = SceneView.lastActiveSceneView.camera;
            return true;
        }

        private void OnDrawGizmos()
        {
            Bounds meshBounds = LODGroup.gameObject.GetObjectBounds();
            var corners = meshBounds.GetCorners();


            foreach (var corner in corners)
            {
                Gizmos.DrawSphere(corner, 0.1f);
            }
        }
    }
}