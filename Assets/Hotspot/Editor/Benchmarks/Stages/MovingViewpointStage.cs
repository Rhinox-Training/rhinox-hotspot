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
    public class MovingViewpointStage : BaseBenchmarkStage
    {
        [ForceWideMode, PropertyOrder(-5)]
        public Pose CameraStartPose;

        [ForceWideMode, PropertyOrder(-2)]
        public Pose CameraEndPose;

        protected override IEnumerator RunBenchmarkCoroutine(Action<float> progressCallback = null)
        {
            CameraStartPose.Validate();
            CameraEndPose.Validate();
            
            if (Duration <= float.Epsilon)
            {
                ApplyPoseToCamera(CameraEndPose);
                progressCallback?.Invoke(1.0f);
                yield break;
            }

            ApplyPoseToCamera(CameraStartPose);
            progressCallback?.Invoke(0.0f);
            yield return new WaitForEndOfFrame();

            float progress = 0.0f;
            while (progress < Duration)
            {
                float incrementTime = Time.deltaTime;
                progress += incrementTime;

                float t = Mathf.Clamp01(progress / Duration);
                var lerpedPose = Utility.Lerp(CameraStartPose, CameraEndPose, t);
                ApplyPoseToCamera(lerpedPose);
                progressCallback?.Invoke(t);
                yield return new WaitForEndOfFrame();
            }

            ApplyPoseToCamera(CameraEndPose);
            progressCallback?.Invoke(1.0f);
        }

        [ButtonGroup("Group1", order: -4), Button, EnableIf(nameof(_shouldEnableAlignWithViewButton))]
        public void AlignWithView()
        {
            CameraStartPose = SceneView.lastActiveSceneView.camera.transform.GetWorldPose();
        }

        [ButtonGroup("Group1", order: -4), Button, EnableIf(nameof(_shouldEnableAlignWithSelectionButton))]
        public void AlignWithSelectedObject()
        {
            CameraStartPose = Selection.activeGameObject.transform.GetWorldPose();
        }

        [ButtonGroup("Group2", order: -1), Button("Align With View"), EnableIf(nameof(_shouldEnableAlignWithViewButton))]
        public void AlignWithView2()
        {
            CameraEndPose = SceneView.lastActiveSceneView.camera.transform.GetWorldPose();
        }

        [ButtonGroup("Group2", order: -1), Button("Align With Selected Object"), EnableIf(nameof(_shouldEnableAlignWithSelectionButton))]
        public void AlignWithSelectedObject2()
        {
            CameraEndPose = Selection.activeGameObject.transform.GetWorldPose();
        }

        private bool _shouldEnableAlignWithViewButton => SceneView.lastActiveSceneView != null;


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