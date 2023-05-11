using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
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