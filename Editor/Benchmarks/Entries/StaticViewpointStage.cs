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
            ApplyPoseToCamera(CameraPose);
            float progress = 0.0f;
            var enumeration = SplitByIncrement(Duration).Enumerate();
            foreach (float increment in enumeration)
            {
                progress += increment;
                Debug.Log($"{this} - {progress} seconds {progress/Duration}");
                progressCallback?.Invoke(progress / Duration);
                yield return new WaitForSeconds(increment);
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