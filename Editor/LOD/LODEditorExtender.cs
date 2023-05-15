using System;
using System.Collections.Generic;
using System.Linq;
using Hotspot.Utils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Magnus;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    [CustomEditor(typeof(LODGroup))]
    public class LODEditorExtender : DefaultEditorExtender<LODGroup>
    {
        private float _maxDensityPerLOD = 0.01f;
        private int _amountOfDensitySamples = 10;
        private float _sampleOffset = 0.05f;

        public override void OnInspectorGUI()
        {
            //TODO: Add bool to project settings to disable extensions
            base.OnInspectorGUI();

            GUILayout.Space(10);
            CustomEditorGUI.Title("LOD Optimization", EditorStyles.boldLabel);

            CustomEditorGUI.Title("Settings");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max density per LOD: ");
            _maxDensityPerLOD = EditorGUILayout.FloatField(_maxDensityPerLOD);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Amount of density samples: ");
            _amountOfDensitySamples = EditorGUILayout.IntField(_amountOfDensitySamples);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Camera offset per sample: ");
            _sampleOffset = EditorGUILayout.FloatField(_sampleOffset);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            CustomEditorGUI.Title("Execute");
            if (GUILayout.Button("Optimize LODs"))
            {
                LODGroup lodGroup = (LODGroup)target;
                OptimizeLODs(lodGroup);
            }

            if (GUILayout.Button("Test vertex density"))
            {
                LODGroup lodGroup = (LODGroup)target;
                TestVertexDensity(lodGroup);
            }

            if (GUILayout.Button("Test"))
            {
                Test((LODGroup)target);
            }
        }

        private void OptimizeLODs(LODGroup lodGroup)
        {
            var lods = lodGroup.GetLODs();
            int currentLOD = 0;
            if (!TryGetMainCamera(out var camera))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,OptimizeLODs] Could not get main camera");
                return;
            }
            
        }

        private void Test(Component lodGroup)
        {
            Camera mainCamera = Camera.main;
            CameraSpoof cameraInfo = new CameraSpoof();
            Vector3 pos = lodGroup.transform.position;
            cameraInfo.SetCameraInfo(mainCamera);

            //------------------------------------------------------------------------------------------------------------
            // COMPARE THE MATRICES
            //------------------------------------------------------------------------------------------------------------
            var ownProjectionMatrix = cameraInfo.GetProjectionMatrix();
            var unityProjectionMatrix = mainCamera.projectionMatrix;
            if (!AreMatricesEqual(ownProjectionMatrix, unityProjectionMatrix))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], Projection matrices are not equal!");
                return;
            }

            var ownViewMatrix = cameraInfo.GetViewMatrix();
            var unityViewMatrix = mainCamera.worldToCameraMatrix;
            if (!AreMatricesEqual(ownViewMatrix, unityViewMatrix))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], View matrices are not equal!");
                return;
            }

            //------------------------------------------------------------------------------------------------------------
            // COMPARE THE FUNCTIONALITY
            //------------------------------------------------------------------------------------------------------------
            var unityViewPortPos = mainCamera.WorldToViewportPoint(pos);
            var ownViewPortPos = cameraInfo.WorldToViewportPoint(pos);
            if (!AreVectorsEqual(unityViewPortPos, ownViewPortPos, 0.01f))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], Viewport positions are not equal!");
                PLog.Info<HotspotLogger>($"Unity Viewport Pos: {unityViewPortPos}");
                PLog.Info<HotspotLogger>($"Camera Info  Viewport Pos: {ownViewPortPos}");
                return;
            }

            var unityScreenPos = mainCamera.WorldToScreenPoint(pos);
            var ownScreenPos = cameraInfo.WorldToScreenPoint(pos);
            if (!AreVectorsEqual(unityScreenPos, ownScreenPos, 0.01f))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], Screen positions are not equal!");
                PLog.Info<HotspotLogger>($"Unity Screen Pos: {unityScreenPos}");
                PLog.Info<HotspotLogger>($"Camera Info  Screen Pos: {ownScreenPos}");
                return;
            }
        }

        public static bool AreVectorsEqual(Vector3 vector1, Vector3 vector2, float tolerance = 0.0001f)
        {
            return Mathf.Abs(vector1.x - vector2.x) <= tolerance &&
                   Mathf.Abs(vector1.y - vector2.y) <= tolerance &&
                   Mathf.Abs(vector1.z - vector2.z) <= tolerance;
        }

        private bool AreMatricesEqual(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (!Mathf.Approximately(matrix1[row, col], matrix2[row, col]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void TestVertexDensity(LODGroup lodGroup)
        {
            if (!TryGetMainCamera(out var mainCamera))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TestVertexDensity] Could not get main camera");
                return;
            }

            CameraSpoof cameraInfo = new CameraSpoof();
            cameraInfo.SetCameraInfo(mainCamera);
            var lods = lodGroup.GetLODs();

            foreach (LOD lod in lods)
            {
                float camDensity = lod.renderers.Sum(renderer =>
                    VertexDensityUtility.CalculateVertexDensity(renderer, mainCamera).Density);
                float spoofDensity = lod.renderers.Sum(renderer =>
                    VertexDensityUtility.CalculateVertexDensity(renderer, cameraInfo).Density);
                if (camDensity - spoofDensity > 0.01f)
                {
                    PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TestVertexDensity] Vertex density is not equal!");
                    return;
                }
            }
        }

        private bool TryGetMainCamera(out Camera camera)
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
                camera = Camera.main;

            if (camera == null)
            {
                PLog.Warn<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                return false;
            }

            return true;
        }
    }
}