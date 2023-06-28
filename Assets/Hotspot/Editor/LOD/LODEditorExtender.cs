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
    /// <summary>
    /// Custom editor for LODGroup component with additional optimization options.
    /// </summary>
    [CustomEditor(typeof(LODGroup))]
    public class LODEditorExtender : DefaultEditorExtender<LODGroup>
    {
        // Set default maximum density per LOD.
        private float _maxDensityPerLOD = 0.01f;

        // Initialize force culling percentage flag as false.
        private bool _forceCullingPercentage = false;

        // Initialize culling percentage as 10.
        private int _cullingPercentage = 10;

        // Keep track of previous LODs for potential reversion.
        private LOD[] _previousLoDs;

        // Scene stage for previewing LODs.
        private LODSceneStage _stage;

        /// <summary>
        /// Overrides the default inspector GUI for this component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Call the base class method.
            base.OnInspectorGUI();

            // Add space in the inspector.
            GUILayout.Space(10);
            // Display the custom title in the inspector.
            CustomEditorGUI.Title("LOD Optimization", EditorStyles.boldLabel);
            // Display settings related to LOD Optimization.
            DisplaySettings();
            // Display available operations for LOD Optimization.
            DisplayExecuteOptions();
        }

        /// <summary>
        /// Displays LOD optimization settings in the inspector.
        /// </summary>
        private void DisplaySettings()
        {
            // Display settings title in the inspector.
            CustomEditorGUI.Title("Settings");

            // Display field for max density per LOD in the inspector.
            _maxDensityPerLOD =
                HotSpotCustomEditorGUI.LabeledFloatField("Max screen vertex density per LOD: ", _maxDensityPerLOD);

            // Display field for force culling percentage toggle in the inspector.
            _forceCullingPercentage =
                HotSpotCustomEditorGUI.LabeledToggle("Force culling percentage: ", _forceCullingPercentage);

            // If culling percentage is forced, display field for culling percentage in the inspector.
            if (_forceCullingPercentage)
            {
                _cullingPercentage = HotSpotCustomEditorGUI.LabeledIntField("Culling percentage: ", _cullingPercentage);
                _cullingPercentage = Mathf.Clamp(_cullingPercentage, 0, 100);
            }
        }

        /// <summary>
        /// Displays execution options for LOD optimization in the inspector.
        /// </summary>
        private void DisplayExecuteOptions()
        {
            // Add space in the inspector.
            GUILayout.Space(5f);
            // Display execute title in the inspector.
            CustomEditorGUI.Title("Execute");
            // Cast the target object to LODGroup.
            LODGroup lodGroup = (LODGroup)target;

            // If optimize LODs button is clicked, call the OptimizeLoDs method.
            if (GUILayout.Button("Optimize LODs"))
            {
                OptimizeLoDs(lodGroup);
            }

            // If previous LODs are available and revert button is clicked, call the RevertLODS method.
            if (_previousLoDs != null && GUILayout.Button("Revert previous LODS"))
            {
                RevertLODS(lodGroup);
            }

            if (GUILayout.Button("Preview LODs"))
            {
                OpenPreviewStage(lodGroup);
            }
        }

        /// <summary>
        /// Opens a preview stage for testing LOD optimization.
        /// </summary>
        /// <param name="lodGroup">The LODGroup to be tested.</param>
        private void OpenPreviewStage(LODGroup lodGroup)
        {
            // Create a new LODSceneStage instance.
            _stage = CreateInstance<LODSceneStage>();
            // Go to the created stage.
            StageUtility.GoToStage(_stage, true);
            // Set the original scene in the stage.
            _stage.SetOriginalScene(SceneManager.GetActiveScene());
            // Setup the scene with the given LODGroup.
            _stage.SetupScene(lodGroup);
        }

        /// <summary>
        /// Reverts the LODGroup to its previous LODs.
        /// </summary>
        /// <param name="lodGroup">The LODGroup to revert.</param>
        private void RevertLODS(LODGroup lodGroup)
        {
            // Set the LODs of the LODGroup to the previously saved LODs.
            lodGroup.SetLODs(_previousLoDs);
            // Clear the previously saved LODs.
            _previousLoDs = null;
        }

        /// <summary>
        /// Optimizes the LODs of a given LODGroup.
        /// </summary>
        /// <param name="lodGroup">The LODGroup to be optimized.</param>
        private void OptimizeLoDs(LODGroup lodGroup)
        {
            // Get current LODs.
            var lods = lodGroup.GetLODs();
            // Get the main camera for density calculations.
            if (!HotSpotUtils.TryGetMainCamera(out var camera))
            {
                PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,OptimizeLODs] Could not get main camera");
                return;
            }

            // Create new optimized LODs.
            var newLods = lods.Select((lod, i) => CreateOptimizedLOD(lod, camera, i == lods.Length - 1));
            // Save current LODs for potential reversion.
            _previousLoDs = lods;
            // Process new LODs before applying them.
            ProcessLods(ref newLods);
            // Apply new LODs to the LODGroup.
            lodGroup.SetLODs(newLods.ToArray());
        }

        /// <summary>
        /// Creates an optimized LOD.
        /// </summary>
        /// <param name="lod">The LOD to be optimized.</param>
        /// <param name="camera">The camera for calculating density.</param>
        /// <param name="isLastLOD">Indicates whether this is the last LOD in the group.</param>
        /// <returns>Returns an optimized LOD.</returns>
        private LOD CreateOptimizedLOD(LOD lod, Camera camera, bool isLastLOD)
        {
            // Create mesh info list for all renderers in the LOD.
            var meshInfoList = lod.renderers
                .Select(MeshInfo.Create);

            // Calculate maximum height percentage based on target density.
            float maxHeightPercentage =
                GetHeightPercentageAtTargetDensity(meshInfoList, camera, _maxDensityPerLOD) / 100;
            // Calculate LOD height based on whether it's the last LOD and whether culling percentage is forced.
            float lodHeight = isLastLOD && _forceCullingPercentage ? _cullingPercentage / 100f : maxHeightPercentage;

            // Create new LOD with calculated height.
            return new LOD(lodHeight, lod.renderers);
        }

        /// <summary>
        /// Processes and validates new LODs.
        /// </summary>
        /// <param name="newLods">The new LODs to be processed.</param>
        private void ProcessLods(ref IEnumerable<LOD> newLods)
        {
            var enumerable = newLods as List<LOD> ?? newLods.ToList();
            // Find LODs with invalid transition height.
            var invalidLods = enumerable.Where(lod => lod.screenRelativeTransitionHeight >= 1f).ToList();

            // Log warning and adjust transition height for invalid LODs.
            invalidLods.ForEach(invalidLOD =>
            {
                int i = enumerable.IndexOf(invalidLOD);
                PLog.Warn<HotspotLogger>(
                    $"[MaterialRenderingAnalysis,ProcessLods] Invalid LOD {i} transition height {invalidLOD.screenRelativeTransitionHeight}. Consider removing.");
                float previousTransitionHeight = i == 0 ? 1f : enumerable[i - 1].screenRelativeTransitionHeight;
                invalidLOD.screenRelativeTransitionHeight = previousTransitionHeight;
            });

            // Remove LODs with invalid transition height.
            enumerable.RemoveAll(lod => lod.screenRelativeTransitionHeight >= 1f);
            
            newLods = enumerable;
        }

        /// <summary>
        /// Calculates the height percentage based on target density.
        /// </summary>
        /// <param name="meshInfoList">The list of mesh information to calculate from.</param>
        /// <param name="camera">The camera for calculating density.</param>
        /// <param name="targetDensity">The target density for LOD optimization.</param>
        /// <returns>Returns the calculated height percentage.</returns>
        private float GetHeightPercentageAtTargetDensity(IEnumerable<MeshInfo> meshInfoList, Camera camera,
            float targetDensity)
        {
            // Calculate total vertices.
            int totalVertices = meshInfoList.Sum(mi => mi.Mesh.vertexCount);

            // Calculate the screen space radius that produces the target density.
            float targetScreenRadius = totalVertices / targetDensity;
            // Calculate height percentage.
            float heightPercentage = targetScreenRadius / camera.pixelRect.height;

            return heightPercentage;
        }
    }
}