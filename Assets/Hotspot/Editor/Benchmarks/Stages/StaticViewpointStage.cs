using System;
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
        [ForceWideMode]
        public Pose CameraPose;

        protected override IEnumerator RunBenchmarkCoroutine(Action<float> progressCallback = null)
        {
            CameraPose.Validate();

            ApplyPoseToCamera(CameraPose);
            float progress = 0.0f;

            while (progress < Duration)
            {
                float incrementTime = Time.deltaTime;
                progress += incrementTime;
                progressCallback?.Invoke(progress / Duration);
                yield return new WaitForEndOfFrame();
            }
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