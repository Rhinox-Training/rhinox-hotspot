using System.Collections;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    public class StaticViewpointStage : BaseBenchmarkStage
    {
        [Range(0.0f, 30.0f)]
        public float Duration;
        
        [ForceWideMode]
        public Pose CameraPose;

        public override bool ValidateInputs()
        {
            if (!base.ValidateInputs())
                return false;
            return Duration > 0.0f && Duration <= 30.0f;
        }

        protected override IEnumerator RunBenchmarkCoroutine()
        {
            ApplyPoseToCamera(CameraPose);
            yield return new WaitForSeconds(Duration);
        }

        [ButtonGroup, Button, EnableIf(nameof(_shouldEnableAlignWithViewButton))]
        public void AlignWithView()
        {
            CameraPose = SceneView.lastActiveSceneView.camera.transform.GetWorldPose();
        }

        private bool _shouldEnableAlignWithViewButton => SceneView.lastActiveSceneView != null;
        
        [ButtonGroup, Button, EnableIf(nameof(_shouldEnableAlignWithSelectionButton))]
        public void AlignWithSelectedObject()
        {
            CameraPose = Selection.activeGameObject.transform.GetWorldPose();
        }

        private bool _shouldEnableAlignWithSelectionButton
        {
            get
            {
                if (Selection.activeGameObject == null || Selection.transforms == null || Selection.transforms.Length > 1)
                    return false;

                return object.Equals(Selection.activeGameObject.scene, EditorSceneManager.GetActiveScene());
            }
        }
    }
}