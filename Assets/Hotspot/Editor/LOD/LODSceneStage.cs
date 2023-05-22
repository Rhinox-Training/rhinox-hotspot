using Rhinox.GUIUtils;
using Rhinox.Lightspeed;
using UnityEditor;
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
        private LODGroupView _groupView;

        protected override void OnEnable()
        {
            base.OnEnable();
            if(_groupView)
                _groupView.CurrentLODChanged += OnGhostLODsChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_groupView)
                _groupView.CurrentLODChanged -= OnGhostLODsChanged;
        }

        public void SetupScene(LODGroup lodGroup)
        {
            CreateSceneEnvironment();
            
            GameObject lodPreview = new GameObject("LOD Preview");
            _groupView = lodPreview.AddComponent<LODGroupView>();
            _groupView.LODGroup = lodGroup;
            _groupView.CurrentLODChanged += OnGhostLODsChanged;
            StageUtility.PlaceGameObjectInCurrentStage(lodPreview);
            
            _lodGroup = lodGroup;
            var transform = _lodGroup.transform;
            _cachedPosition = transform.position;
            transform.position = Vector3.zero;
            
            StageUtility.PlaceGameObjectInCurrentStage(_lodGroup.gameObject);

            SceneView.lastActiveSceneView.Frame(_lodGroup.gameObject.GetObjectBounds());
        }

        private void OnGhostLODsChanged(LODGroupView groupView)
        {
            StageUtility.PlaceGameObjectInCurrentStage(groupView.PreviousLODGhost);
            StageUtility.PlaceGameObjectInCurrentStage(groupView.NextLODGhost);
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
            var gameObject = _lodGroup.gameObject;
            gameObject.transform.position = _cachedPosition;
            SceneManager.MoveGameObjectToScene(gameObject, _originScene);
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