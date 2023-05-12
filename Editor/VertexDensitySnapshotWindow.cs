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
        private Dictionary<Renderer, float> _filteredDensityValues;
        private Dictionary<Renderer, float> _allDensityValues;

        private PageableReorderableList _pageableList;
        private int _itemsPerPage = 10;
        private float _minDensity = 0.1f;
        private float _maxDensity = 100.0f;
        private DistributionInfo _distributionInfo = new DistributionInfo() {Min = 0.0f, Max = 100.0f};

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
            CustomEditorGUI.Title("Snapshot settings:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Max Vertices PerCube: ");
                _maxVerticesPerCube = EditorGUILayout.IntField(_maxVerticesPerCube);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Min Octree CubeSize: ");
                _minOctreeCubeSize = EditorGUILayout.FloatField(_minOctreeCubeSize);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Vertices per pixel threshold: ");
                _vertsPerPixelThreshold = EditorGUILayout.FloatField(_vertsPerPixelThreshold);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Take Snapshot"))
            {
                TakeSnapshot();
            }
            CustomEditorGUI.Title("Distribution");
            if (_allDensityValues != null)
            {
                GUILayout.BeginHorizontal( EditorStyles.helpBox );
                {
                    GUILayout.Space(24.0f);
                    GUIChartEditor.BeginChart(10, 100, 100, 200, Color.black,
                        GUIChartEditorOptions.ChartBounds(0.0f, 1.0f, 0.0f, _allDensityValues.Count / 2.0f),
                        GUIChartEditorOptions.SetOrigin(ChartOrigins.BottomLeft),
                        GUIChartEditorOptions.ShowAxes(Color.white),
                        GUIChartEditorOptions.ShowGrid(0.1f, _allDensityValues.Count / 20.0f, Color.gray, true)
                        /*,GUIChartEditorOptions.ShowLabels("0.##", 1f, 1f, -0.1f, 1f, -0.075f, 1f)*/);
                    _distributionInfo = GUIChartEditor.PushDataToDistribution(_allDensityValues.Values, Color.red);
                    GUIChartEditor.EndChart();
                    GUILayout.Space(4.0f);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            {
                float oldMinDensity = _minDensity;
                float oldMaxDensity = _maxDensity;
                EditorGUILayout.MinMaxSlider("Filter View:", ref _minDensity, ref _maxDensity, Mathf.Max(_distributionInfo.Min, 0.0f), Mathf.Min(_distributionInfo.Max, 100.0f));
                if (!oldMinDensity.LossyEquals(_minDensity) || !oldMaxDensity.LossyEquals(_maxDensity))
                {
                    _filteredDensityValues.Clear();
                    foreach (var r in _allDensityValues.Keys)
                    {
                        float density = _allDensityValues[r];
                        if (density >= _minDensity && density <= _maxDensity)
                            _filteredDensityValues.Add(r, density);
                
                    }
                }
            }
            GUILayout.EndHorizontal();
            if (_filteredDensityValues != null && _filteredDensityValues.Any())
            {
                _pageableList.DoLayoutList(GUIContent.none);
            }

        }

        private void TakeSnapshot()
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
                    mainCamera = Camera.main;
                }
            }
            else
                mainCamera = SceneView.GetAllSceneCameras()[0];

            var visibleRenderersInView = _renderers.Where(x => x != null && x.isVisible && x.IsWithinFrustum(mainCamera));
            _allDensityValues = new Dictionary<Renderer, float>();
            _filteredDensityValues = new Dictionary<Renderer, float>();
            foreach (var r in visibleRenderersInView)
            {
                float density = _octreeBuilder.GetRenderedVertexDensity(r, mainCamera);
                if (density > _minDensity)
                    _filteredDensityValues.Add(r, density);
                _allDensityValues.Add(r, density);
                
            }

            // Fetch the visible renderers and the density values
            // var value = (_renderers
            //     .Where(x => x != null && x.isVisible && x.IsWithinFrustum(mainCamera))
            //     .Select(r =>
            //         new KeyValuePair<Renderer, float>(r, _octreeBuilder.GetRenderedVertexDensity(r, mainCamera)))
            //     .Where(x => x.Value > _minDensity)
            //     .OrderByDescending(x => x.Value));
            //
            // _filteredDensityValues = value.ToDictionary(x => x.Key, x => x.Value);
            
            // Create the pageable list
            _pageableList = new PageableReorderableList(_filteredDensityValues.Select(x => x).OrderByDescending(x => x.Value).ToList())
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
            var element = _filteredDensityValues.ElementAt(index);
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