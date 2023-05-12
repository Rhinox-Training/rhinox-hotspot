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

        private void Test(LODGroup lodGroup)
        {
            Camera mainCamera = Camera.main;
            Utils.CameraInfo cameraInfo = new Utils.CameraInfo();
            Vector3 pos = lodGroup.transform.position;
            cameraInfo.SetCameraInfo(mainCamera);
            
            var temp = mainCamera.worldToCameraMatrix;
            var temp2 = cameraInfo.GetViewMatrix();
            CompareMatrices(temp, temp2);
            
            var temp3 = mainCamera.projectionMatrix;
            var temp4 = cameraInfo.GetProjectionMatrix();
            CompareMatrices(temp3, temp4);
        }

        private static void CompareMatrices(Matrix4x4 m1, Matrix4x4 m2)
        {
            var unityCol0 = m1.GetColumn(0);
            var unityCol1 = m1.GetColumn(1);
            var unityCol2 = m1.GetColumn(2);
            var unityCol3 = m1.GetColumn(3);

            var camInfoCol0 = m2.GetColumn(0);
            var camInfoCol1 = m2.GetColumn(1);
            var camInfoCol2 = m2.GetColumn(2);
            var camInfoCol3 = m2.GetColumn(3);

            if (!unityCol0.Equals(camInfoCol0))
                Debug.LogWarning("First Column not equal");
            if (!unityCol1.Equals(camInfoCol1))
                Debug.LogWarning("Second Column not equal");
            if (!unityCol2.Equals(camInfoCol2))
                Debug.LogWarning("Third Column not equal");
            if (!unityCol3.Equals(camInfoCol3))
                Debug.LogWarning("Fourth Column not equal");
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
            if (Application.isPlaying)
            {
                if (CameraInfo.Instance == null)
                {
                    PLog.Error<HotspotLogger>(
                        "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
                    return;
                }

                mainCamera = CameraInfo.Instance.Main;

                if (mainCamera == null)
                {
                    PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                    mainCamera = Camera.main;
                }
            }
            else
                mainCamera = SceneView.GetAllSceneCameras()[0];

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