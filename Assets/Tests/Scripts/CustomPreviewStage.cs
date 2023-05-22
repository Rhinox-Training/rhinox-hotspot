using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Scripts
{
    public class CustomPreviewStage : PreviewSceneStage
    {
        public PreviewWindow OwnerWindow;
        public GUIContent HeaderContent;
        public SceneView OpenedSceneView;

        public void SetupScene()
        {
            GameObject light = new GameObject("Lighting");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.position = new Vector3(50,-30,0);
            
            StageUtility.PlaceGameObjectInCurrentStage(light);
            // OpenedSceneView.FrameSelected();
        }
        
        protected override GUIContent CreateHeaderContent()
        {
            GUIContent headerContent = new GUIContent()
            {
                text = "Custom Preview Stage",
                image = EditorGUIUtility.IconContent("GameObject Icon").image
            };
            return headerContent;
        }

        public void SetScene(Scene scene)
        {
            this.scene = scene;
        }
    }
}