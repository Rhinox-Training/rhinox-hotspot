using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
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

        private Dictionary<LOD, float> _densityValues;

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


            GUILayout.Space(5f);
            CustomEditorGUI.Title("Execute");
            if (GUILayout.Button("Optimize LODs"))
            {
                LODGroup lodGroup = (LODGroup)target;
                OptimizeLODs(lodGroup);
            }

            if (GUILayout.Button("Get vertex densities"))
            {
                LODGroup lod = (LODGroup)target;
                GetVertexDensity(lod);
            }

            if (GUILayout.Button("Test"))
            {
                LODGroup lod = (LODGroup)target;
                Test(lod);
            }
        }

        private void Test(Component lodGroup)
        {
            Camera mainCamera = Camera.main;
            Utils.CameraInfo cameraInfo = new Utils.CameraInfo();
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
            if (!unityViewPortPos.Equals(ownViewPortPos))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], Viewport positions are not equal!");
                PLog.Info<HotspotLogger>($"Unity Viewport Pos: {unityViewPortPos}");
                PLog.Info<HotspotLogger>($"Camera Info  Viewport Pos: {ownViewPortPos}");
                return;
            }
            
            var unityScreenPos = mainCamera.WorldToScreenPoint(pos);   
            var ownScreenPos = cameraInfo.WorldToScreenPoint(pos);
            if (!unityScreenPos.Equals(ownScreenPos))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis:Test], Screen positions are not equal!");
                PLog.Info<HotspotLogger>($"Unity Screen Pos: {unityScreenPos}");
                PLog.Info<HotspotLogger>($"Camera Info  Screen Pos: {ownScreenPos}");
                return;
            }
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

        private void RunTest(LODGroup lodGroup)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
            }

            const int sampleSize = 15;
            const float sampleDistanceOffset = 5f;

            var transform = mainCamera.transform;
            transform.LookAt(lodGroup.gameObject.transform);
            List<float> distances = new List<float>();
            List<List<float>> sampleEntries = new();
            for (int sampleID = 0; sampleID < sampleSize; sampleID++)
            {
                distances.Add(Vector3.Distance(transform.position, lodGroup.gameObject.transform.position));
                sampleEntries.Add(new List<float>());
                foreach (LOD lod in lodGroup.GetLODs())
                {
                    float avgDensity = lod.renderers.Sum(renderer =>
                        VertexDensityUtility.CalculateVertexDensity(renderer, mainCamera).Density);
                    avgDensity /= lod.renderers.Length;
                    sampleEntries[sampleID].Add(avgDensity);
                }

                transform.position -= transform.forward * sampleDistanceOffset;
            }

            var table = new DataTable();
            table.Columns.Add("Distance");
            for (int i = 0; i < lodGroup.GetLODs().Length; i++)
            {
                table.Columns.Add($"LOD {i}");
            }

            for (int i = 0; i < sampleSize; i++)
            {
                var row = table.NewRow();
                row["Distance"] = distances[i];
                for (int j = 0; j < lodGroup.GetLODs().Length; j++)
                {
                    row[$"LOD {j}"] = sampleEntries[i][j];
                }

                table.Rows.Add(row);
            }

            string csvFileStr = table.ToCsv();
            FileInfo info = new FileInfo("DensityTest.csv");
            FileHelper.CreateDirectoryIfNotExists(info.DirectoryName);
            File.WriteAllText("DensityTest.csv", csvFileStr);
        }

        private void OptimizeLODs(LODGroup lodGroup)
        {
            if (_densityValues == null)
                GetVertexDensity(lodGroup);
        }

        private void GetVertexDensity(LODGroup lodGroup)
        {
            Camera mainCamera;
            // if (Application.isPlaying)
            // {
            //     if (CameraInfo.Instance == null)
            //     {
            //         PLog.Error<HotspotLogger>(
            //             "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
            //         return;
            //     }
            //
            //     mainCamera = CameraInfo.Instance.Main;
            //
            //     if (mainCamera == null)
            //     {
            //         PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
            //         mainCamera = Camera.main;
            //     }
            // }
            // else
            //     mainCamera = SceneView.GetAllSceneCameras()[0];
            mainCamera = Camera.main;

            var lods = lodGroup.GetLODs();
            _densityValues = new Dictionary<LOD, float>();

            foreach (LOD lod in lods)
            {
                float avgDensity = lod.renderers.Sum(renderer =>
                    VertexDensityUtility.CalculateVertexDensity(renderer, mainCamera).Density);
                avgDensity /= lod.renderers.Length;
                _densityValues.Add(lod, avgDensity);
            }

            foreach (var pair in _densityValues)
            {
                Debug.Log($"LOD: {pair.Key.ToString()}, Avg density: {pair.Value}");
            }
        }
    }
}