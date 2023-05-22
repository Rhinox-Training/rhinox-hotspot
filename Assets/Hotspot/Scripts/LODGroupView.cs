using System;
using UnityEngine;

namespace Hotspot
{
    /// <summary>
    /// Provides a preview view for LOD (Level of Detail) transitions in Unity's LODGroup.
    /// </summary>
    [ExecuteInEditMode]
    public class LODGroupView : MonoBehaviour
    {
        [SerializeField] private LODGroup _lodGroup;
        [SerializeField] private GameObject _previousLODGhost;
        [SerializeField] private GameObject _nextLODGhost;

        // Event raised when the current LOD has changed
        public Action<LODGroupView> CurrentLODChanged;

        private Camera _mainCamera;
        private int _currentLOD = 0;
        private int _currentLODPercentage = 0;

        /// <summary>
        /// Gets or sets the LODGroup associated with this ghost view.
        /// When setting, also checks and sets LOD if the value is not null.
        /// </summary>
        public LODGroup LODGroup
        {
            get => _lodGroup;
            set
            {
                if (value == null)
                    return;

                _lodGroup = value;
                CheckAndSetLOD();
            }
        }

        public GameObject PreviousLODGhost => _previousLODGhost;
        public GameObject NextLODGhost => _nextLODGhost;

        /// <summary>
        /// Invoked when the script instance is being destroyed.
        /// Immediately destroys next and previous LOD ghost objects.
        /// </summary>
        private void OnDestroy()
        {
            DestroyImmediate(NextLODGhost);
            DestroyImmediate(PreviousLODGhost);
        }

        /// <summary>
        /// Invoked every frame, checks and sets LOD.
        /// </summary>
        private void Update()
        {
            CheckAndSetLOD();
        }

        /// <summary>
        /// Checks and sets the Level of Detail (LOD).
        /// If main camera is not initialized, initializes it first.
        /// </summary>
        private void CheckAndSetLOD()
        {
            if (_mainCamera == null)
            {
                Initialize();
                return;
            }

            if (_lodGroup == null)
                return;

            float lodPercentage = _lodGroup.CalculateCurrentHeightPercentage(_mainCamera);
            if (!Mathf.Approximately(lodPercentage, _currentLODPercentage))
            {
                _currentLODPercentage = (int)lodPercentage;
                SetLOD();
            }
        }

        /// <summary>
        /// Sets the Level of Detail (LOD) and updates ghost objects if the LOD has changed.
        /// </summary>
        private void SetLOD()
        {
            if (_lodGroup == null)
                return;

            int newLOD = _lodGroup.GetCurrentLODIndex(_mainCamera);
            if (newLOD == _currentLOD)
                return;

            _currentLOD = newLOD;

            SpawnLODPreviews();

            CurrentLODChanged?.Invoke(this);
        }

        /// <summary>
        /// Instantiates ghost objects for the next and previous LODs.
        /// </summary>
        private void SpawnLODPreviews()
        {
            DestroyImmediate(_previousLODGhost);
            DestroyImmediate(_nextLODGhost);

            LOD[] lods = _lodGroup.GetLODs();
            int previousLODIdx = _currentLOD - 1;
            int nextLODIdx = _currentLOD + 1;
            Transform lodTransform = _lodGroup.transform;
            var right = lodTransform.right;
            SpawnLODGameObject(lods, previousLODIdx, "Preceding LOD", -right);
            SpawnLODGameObject(lods, nextLODIdx, "Following LOD", right);
        }

        /// <summary>
        /// Spawns a new LOD game object if the index is within the bounds of the LOD array.
        /// </summary>
        private void SpawnLODGameObject(LOD[] lods, int index, string goName, Vector3 offsetDirection)
        {
            if (index >= 0 && index < lods.Length)
            {
                var newLODGhost = new GameObject(goName);
                foreach (Renderer currentRenderer in lods[index].renderers)
                    Instantiate(currentRenderer.gameObject, newLODGhost.transform, true);

                newLODGhost.transform.position = _lodGroup.transform.position + offsetDirection * _lodGroup.size;
                if (goName == "Preceding LOD") _previousLODGhost = newLODGhost;
                else _nextLODGhost = newLODGhost;
            }
        }

        private void Initialize()
        {
            if (_mainCamera == null)
                HotSpotUtils.TryGetMainCamera(out _mainCamera);
        }
    }
}