using System;
using System.Collections;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hotspot.Editor
{
    public class StaticRotatingViewpointStage : BaseBenchmarkStage
    {
        [ForceWideMode] public Pose CameraPose;

        public float MinRelativeYawDegrees = -30.0f;
        public float MaxRelativeYawDegrees = 30.0f;

        protected override IEnumerator RunBenchmarkCoroutine(Action<float> progressCallback = null)
        {
            Pose startPose = new Pose(CameraPose.position,
                CameraPose.rotation * Quaternion.AngleAxis(MinRelativeYawDegrees, Vector3.up));
            Pose endPose = new Pose(CameraPose.position,
                CameraPose.rotation * Quaternion.AngleAxis(MaxRelativeYawDegrees, Vector3.up));
            
            if (Duration <= float.Epsilon)
            {
                ApplyPoseToCamera(endPose);
                progressCallback?.Invoke(1.0f);
                yield break;
            }
            
            ApplyPoseToCamera(startPose);
            progressCallback?.Invoke(0.0f);
            yield return new WaitForEndOfFrame();
            
            float progress = 0.0f;
            while (progress < Duration)
            {
                float incrementTime = Time.deltaTime;
                progress += incrementTime;

                float t = Mathf.Clamp01(progress / Duration);
                var lerpedPose = Utility.Lerp(startPose, endPose, t);
                ApplyPoseToCamera(lerpedPose);
                progressCallback?.Invoke(t);
                yield return new WaitForEndOfFrame();
            }

            ApplyPoseToCamera(endPose);
            progressCallback?.Invoke(1.0f);
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
                if (Selection.activeGameObject == null || Selection.transforms == null ||
                    Selection.transforms.Length > 1)
                    return false;

                return object.Equals(Selection.activeGameObject.scene, EditorSceneManager.GetActiveScene());
            }
        }
    }
}