using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Magnus;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hotspot.Editor
{
    public class VertexDensitySnapshotWindow : CustomEditorWindow
    {
        private Renderer[] _renderers;

        private VertexOctreeBuilder _octreeBuilder;

        private int _maxVerticesPerCube = 500;
        private float _minOctreeCubeSize = 1f;
        private float _vertsPerPixelThreshold = 4f;
        private Dictionary<Renderer, float> _densityValues;

        private PageableReorderableList _pageableList;
        private int _itemsPerPage = 10;
        private float _minDensity = 0.1f;

        [MenuItem(HotspotWindowHelper.ANALYSIS_PREFIX + "Vertex Density Snapshot", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(VertexDensitySnapshotWindow));
            win.titleContent = new GUIContent("Vertex Pixel Density Snapshot");
        }

        protected override void Initialize()
        {
            _octreeBuilder = new VertexOctreeBuilder(_maxVerticesPerCube, _minOctreeCubeSize, _vertsPerPixelThreshold);
            _octreeBuilder.CreateOctree(true);
            RefreshMeshRendererCache();
        }

        private void RefreshMeshRendererCache()
        {
            _renderers = FindObjectsOfType<Renderer>();
        }

        protected override void OnGUI()
        {
            Profiler.BeginSample("VertexDensity");
            CustomEditorGUI.Title("Snapshot settings:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min vertex density: ");
            _minDensity = EditorGUILayout.FloatField(_minDensity);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Vertices PerCube: ");
            _maxVerticesPerCube = EditorGUILayout.IntField(_maxVerticesPerCube);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Octree CubeSize: ");
            _minOctreeCubeSize = EditorGUILayout.FloatField(_minOctreeCubeSize);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vertices per pixel threshold: ");
            _vertsPerPixelThreshold = EditorGUILayout.FloatField(_vertsPerPixelThreshold);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Take Snapshot"))
            {
                TakeSnapshot();
            }

            if (_densityValues == null || !_densityValues.Any())
            {
                Profiler.EndSample();
                return;
            }
            
            _pageableList.DoLayoutList(GUIContent.none);

            Profiler.EndSample();
        }

        protected override void TakeSnapshot()
        {
            _octreeBuilder = new VertexOctreeBuilder(_maxVerticesPerCube, _minOctreeCubeSize, _vertsPerPixelThreshold);
            _octreeBuilder.CreateOctree(true);

            Camera mainCamera;
            if (Application.isPlaying)
            {
                if (CameraInfo.Instance == null)
                {
                    PLog.Error<HotspotLogger>(
                        "[MaterialRenderingAnalysis,TakeMaterialSnapshot] CameraInfo.Instance is null");
                    return;
                }

                mainCamera = CameraInfo.Instance.Main;

                if (mainCamera == null)
                {
                    PLog.Error<HotspotLogger>("[MaterialRenderingAnalysis,TakeMaterialSnapshot] mainCamera is null");
                    return;
                }
            }
            else
                mainCamera = SceneView.GetAllSceneCameras()[0];

            // Fetch the visible renderers and the density values
            var value = (_renderers
                .Where(x => x != null && x.isVisible && x.IsWithinFrustum(mainCamera))
                .Select(r =>
                    new KeyValuePair<Renderer, float>(r, _octreeBuilder.GetRenderedVertexDensity(r, mainCamera)))
                .Where(x => x.Value > _minDensity)
                .OrderByDescending(x => x.Value));
            
            _densityValues = value.ToDictionary(x => x.Key, x => x.Value);
            
            // Create the pageable list
            _pageableList = new PageableReorderableList(_densityValues.ToList())
            {
                MaxItemsPerPage = _itemsPerPage,
                DisplayAdd = false,
                DisplayRemove = false,
                Draggable = false
            };
            _pageableList.drawElementCallback += OnDrawElement;
        }

        private void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var element = _densityValues.ElementAt(index);
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Label(element.Value.ToString());
            GUILayout.Label(element.Key.gameObject.GetFullName());
            if (GUILayout.Button("Go to object"))
            {
                EditorGUIUtility.PingObject(element.Key.gameObject);
                SceneView.lastActiveSceneView.Frame(element.Key.gameObject.GetObjectBounds(), false);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}