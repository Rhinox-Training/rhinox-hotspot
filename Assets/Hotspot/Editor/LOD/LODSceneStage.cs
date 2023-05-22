using Rhinox.GUIUtils;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    public class LODSceneStage : PreviewSceneStage
    {
        private LODGroup _lodGroup = null;
        private Scene _originScene;
        private Vector3 _cachedPosition = Vector3.zero;

        public void SetupScene(LODGroup lodGroup)
        {
            CreateSceneEnvironment();
            
            GameObject lodPreview = new GameObject("LOD Preview");
            var ghostView = lodPreview.AddComponent<LODGhostView>();
            ghostView.LODGroup = lodGroup;
            StageUtility.PlaceGameObjectInCurrentStage(lodPreview);
            
            _lodGroup = lodGroup;
            _cachedPosition = _lodGroup.transform.position;
            _lodGroup.transform.position = Vector3.zero;
            
            StageUtility.PlaceGameObjectInCurrentStage(_lodGroup.gameObject);
        }

        private void CreateSceneEnvironment()
        {
            GameObject light = new GameObject("Lighting");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(30, 0, 180);
            StageUtility.PlaceGameObjectInCurrentStage(light);
        }

        protected override void OnCloseStage()
        {
            _lodGroup.gameObject.transform.position = _cachedPosition;
            SceneManager.MoveGameObjectToScene(_lodGroup.gameObject, _originScene);
            base.OnCloseStage();
        }

        protected override GUIContent CreateHeaderContent()
        {
            return GUIContentHelper.TempContent("LOD Optimization");
        }

        public void SetOriginalScene(Scene sceneRef)
        {
            _originScene = sceneRef;
        }
    }
}