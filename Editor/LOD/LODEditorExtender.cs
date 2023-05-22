using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    [CustomEditor(typeof(LODGroup))]
    public class LODEditorExtender : DefaultEditorExtender<LODGroup>
    {
        private float _maxDensityPerLOD = 0.01f;
        private bool _forceCullingPercentage = false;
        private int _cullingPercentage = 10;

        LOD[] _previousLODs = null;
        private LODSceneStage _stage;

        public override void OnInspectorGUI()
        {
            //TODO: Add bool to project settings to disable extensions
            base.OnInspectorGUI();

            GUILayout.Space(10);
            CustomEditorGUI.Title("LOD Optimization", EditorStyles.boldLabel);

            CustomEditorGUI.Title("Settings");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max screen vertex density per LOD: ");
            _maxDensityPerLOD = EditorGUILayout.FloatField(_maxDensityPerLOD);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Force culling percentage: ");
            _forceCullingPercentage = EditorGUILayout.Toggle(_forceCullingPercentage);
            GUILayout.EndHorizontal();

            if (_forceCullingPercentage)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Culling percentage: ");
                _cullingPercentage = EditorGUILayout.IntField(_cullingPercentage);
                _cullingPercentage = Mathf.Clamp(_cullingPercentage, 0, 100);
                GUILayout.EndHorizontal();
            }


            GUILayout.Space(5f);
            CustomEditorGUI.Title("Execute");
            if (GUILayout.Button("Optimize LODs"))
            {
                LODGroup lodGroup = (LODGroup)target;
                OptimizeLoDs(lodGroup);
            }

            if (_previousLODs != null)
            {
                if (GUILayout.Button("Revert previous LODS"))
                {
                    LODGroup lodGroup = (LODGroup)target;
                    RevertLODS(lodGroup);
                }
            }

            if (GUILayout.Button("Test in preview stage"))
            {
                LODGroup lodGroup = (LODGroup)target;

                OpenPreviewStage(lodGroup);
            }
        }

        private void OpenPreviewStage(LODGroup lodGroup)
        {
            _stage = CreateInstance<LODSceneStage>();
            StageUtility.GoToStage(_stage, true);
            _stage.SetOriginalScene(SceneManager.GetActiveScene());
            _stage.SetupScene(lodGroup);
        }

        private void RevertLODS(LODGroup lodGroup)
        {
            lodGroup.SetLODs(_previousLODs);
            _previousLODs = null;
        }

        private void OptimizeLoDs(LODGroup lodGroup)
        {
            var lods = lodGroup.GetLODs();
            if (!HotSpotUtils.TryGetMainCamera(out var camera))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,OptimizeLODs] Could not get main camera");
                return;
            }

            var newLods = new List<LOD>();

            for (int i = 0; i < lods.Length; i++)
            {
                var meshInfoList = new List<MeshInfo>();

                foreach (Renderer lodRenderer in lods[i].renderers)
                {
                    if (!MeshInfo.TryCreate(lodRenderer, out MeshInfo mi))
                    {
                        continue;
                    }

                    meshInfoList.Add(mi);
                }

                float maxHeightPercentage =
                    GetHeightPercentageAtTargetDensity(meshInfoList.ToArray(), camera, _maxDensityPerLOD);
                if (_forceCullingPercentage && i == lods.Length - 1)
                    newLods.Add(new LOD(_cullingPercentage / 100f, lods[i].renderers));
                else
                    newLods.Add(new LOD(maxHeightPercentage, lods[i].renderers));
            }

            _previousLODs = lods;
            ProcessLods(newLods);
            lodGroup.SetLODs(newLods.ToArray());
        }

        private void ProcessLods(List<LOD> newLods)
        {
            var invalidLods = newLods
                .Where(x => x.screenRelativeTransitionHeight >= 1f).ToList();

            for (int i = 0; i < newLods.Count; i++)
            {
                if (!invalidLods.Contains(newLods[i]))
                    continue;
                PLog.Warn<HotspotLogger>(
                    $"[MaterialRenderingAnalysis,ProcessLods] Invalid LOD {i} transition height {newLods[i].screenRelativeTransitionHeight}. Consider removing.");
                float previousTransitionHeight = GetTransitionHeightPredecessor(newLods, i);
                newLods[i] = new LOD(previousTransitionHeight, newLods[i].renderers);
            }

            newLods.RemoveAll(x => x.screenRelativeTransitionHeight >= 1f);
        }

        private float GetTransitionHeightPredecessor(List<LOD> newLods, int i)
        {
            return i == 0 ? 1f : newLods[i - 1].screenRelativeTransitionHeight;
        }

        private float GetHeightPercentageAtTargetDensity(MeshInfo[] meshInfoList, Camera camera, float targetDensity)
        {
            // Sum of the vertex counts
            int amountOfVertices = meshInfoList.Select(mi => mi.Mesh.vertexCount).Sum();

            // Calculate the screen space radius that produces the target density
            float targetScreenRadius = amountOfVertices / targetDensity;
            float heightPercentage = targetScreenRadius / camera.pixelRect.height;

            return heightPercentage;
        }
    }
}