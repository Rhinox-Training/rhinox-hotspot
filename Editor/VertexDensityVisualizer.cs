using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Rhinox.Utilities.Editor;
using Rhinox.Lightspeed;
using Hotspot.Editor;
//using static PlasticGui.PlasticTableColumn;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.Hotspot.Editor
{
    public class VertexDensityVisualizer : CustomSceneOverlayWindow<VertexDensityVisualizer>
    {
        protected override string Name => "Vertex Density Visualizer";
        private const string _menuItemPath = WindowHelper.ToolsPrefix + "Show Vertex Density Visualizer";

        private const float _minCubeDistance = 10f;
        private const float _maxCubeDistance = 200f;
        private static PersistentValue<float> _cubeViewDistance;// = 100f;

        private const int _maxVerticesMultiplier = 10;
        private static PersistentValue<int> _MaxVerticesPerCube;//= 500;
        private static PersistentValue<float> _minOctreeCubeSize;// = 1f;


        private Octree _tree = null;
        private DenseVertexSpotWindow _denseVertexSpotInfoWindow = null;


        [MenuItem(_menuItemPath, false, -189)]
        public static void SetupWindow() => Window.Setup();

        [MenuItem(_menuItemPath, true)]
        public static bool SetupValidateWindow() => Window.HandleValidateWindow();

        protected override string GetMenuPath() => _menuItemPath;

        protected override void Initialize()
        {
            _MaxVerticesPerCube = PersistentValue<int>.Create(typeof(VertexDensityVisualizer), nameof(_MaxVerticesPerCube), 500);
            _minOctreeCubeSize = PersistentValue<float>.Create(typeof(VertexDensityVisualizer), nameof(_minOctreeCubeSize), 1f);
            _cubeViewDistance = PersistentValue<float>.Create(typeof(VertexDensityVisualizer), nameof(_cubeViewDistance), 100f);

            base.Initialize();
        }

        private void CreateOctree()
        {
            //FindObjectsOfType is only loaded and active
            //FindObjectsOfTypeAll also includes non-active
            var renderers = Object.FindObjectsOfType<MeshRenderer>();
            var filters = Object.FindObjectsOfType<MeshFilter>();

            if (renderers.Length == 0 || filters.Length == 0)
                return;

            Bounds sceneBound = new Bounds();
            foreach (MeshRenderer meshRenderer in renderers)
            {
                if (!LODRendererCache.IsLOD(meshRenderer))
                    sceneBound.Encapsulate(meshRenderer.bounds);
            }

            float biggestSide = Mathf.Max(Mathf.Max(sceneBound.size.x, sceneBound.size.y), sceneBound.size.z);
            sceneBound.size = new Vector3(biggestSide, biggestSide, biggestSide);

            _tree = new Octree(sceneBound, _MaxVerticesPerCube, _minOctreeCubeSize);

            foreach (MeshFilter meshFilter in filters)
            {
                //first check if the meshfilters renderer is and LOD, if so, discard and goto next
                if (!meshFilter.gameObject.TryGetComponent<Renderer>(out var renderer))
                    continue;

                if (LODRendererCache.IsLOD(renderer))
                    continue;

                foreach (var point in meshFilter.sharedMesh.vertices)
                {
                    _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point));
                }
            }
        }

        protected override void OnSelectionChanged()
        {
            if (_denseVertexSpotInfoWindow != null)
                _denseVertexSpotInfoWindow.UpdateTreshold(_MaxVerticesPerCube);
        }

        protected override void OnGUI()
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Max vertices per cube:");
            //GUILayout.Space(5f);

            _MaxVerticesPerCube.ShowField("Max vertices per cube:");
            //int.TryParse(GUILayout.TextField($"{_MaxVerticesPerCube}", GUILayout.Width(75f)), out _MaxVerticesPerCube);
            if (_MaxVerticesPerCube <= 0)
                _MaxVerticesPerCube.Set(1);
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Min cube size:");
            //float.TryParse(GUILayout.TextField($"{_minOctreeCubeSize}", GUILayout.Width(75f)), out _minOctreeCubeSize);
            _minOctreeCubeSize.ShowField("Min cube size:");
            if (_minOctreeCubeSize <= 0)
                _minOctreeCubeSize.Set(0.001f);
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cube render distance:");
            _cubeViewDistance.Set(GUILayout.HorizontalSlider(_cubeViewDistance, _minCubeDistance, _maxCubeDistance, GUILayout.Width(75f)));
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (GUILayout.Button("Calculate and visualize"))
            {
                CreateOctree();
            }

            GUILayout.Space(5f);

            if (GUILayout.Button("Show Dense Spot Info"))
            {
                if (_tree != null)
                {
                    _denseVertexSpotInfoWindow = EditorWindow.GetWindow<DenseVertexSpotWindow>();

                    if (_denseVertexSpotInfoWindow == null)
                        _denseVertexSpotInfoWindow = new DenseVertexSpotWindow();

                    _denseVertexSpotInfoWindow.UpdateTree(_tree);
                    _denseVertexSpotInfoWindow.UpdateTreshold(_MaxVerticesPerCube);

                    DenseVertexSpotWindow.ShowWindow();
                }
            }


            GUILayout.EndVertical();
        }

        private void DrawChildren(Octree tree)
        {
            if (tree.children == null)
            {
                //check if cube center is too far from camera, if so DISCARD IT
                if (tree.VertexCount > _MaxVerticesPerCube &&
                    (tree._bounds.center.SqrDistanceTo(SceneView.currentDrawingSceneView.camera.transform.position)) <= _cubeViewDistance)
                {
                    using (new eUtility.HandleColor(Color.Lerp(Color.white, Color.red, (tree.VertexCount - _MaxVerticesPerCube) / (_MaxVerticesPerCube * _maxVerticesMultiplier))))
                    {
                        Handles.Label(tree._bounds.center, $"{tree.VertexCount}");
                        Handles.DrawWireCube(tree._bounds.center, tree._bounds.size);
                    }
                }

                return;
            }

            foreach (var child in tree.children)
            {
                DrawChildren(child);
            }
        }

        protected override void OnSceneGUI(SceneView sceneView)
        {
            base.OnSceneGUI(sceneView);

            if (_tree == null)
                return;

            DrawChildren(_tree);

            if (_tree.children != null)
            {
                var recto = BoundsExtensions.ToScreenSpace(_tree.children[0]._bounds, Camera.main);
                GUI.Label(recto, "");
            }
        }
    }

    public class Octree
    {
        private readonly float _minSize;
        private readonly int _maxPoints;
        public Bounds _bounds { get; private set; }

        public int VertexCount => _vertices.Count;

        private List<Vector3> _vertices;
        public Octree[] children { get; private set; }

        public Octree(Bounds bounds, int maxPoints, float minOctSize = 0.1f)
        {
            _minSize = minOctSize;
            _bounds = bounds;
            _maxPoints = maxPoints;
            _vertices = new List<Vector3>();
        }

        public void Insert(Vector3 point)
        {
            if (children != null)
            {
                int index = GetChildIndex(point);
                children[index].Insert(point);
                return;
            }

            _vertices.Add(point);

            if (_vertices.Count > _maxPoints && _bounds.size.x > _minSize)
            {
                Split(_minSize);
            }
        }

        private void Split(float minSize)
        {
            children = new Octree[8];

            float childSize = _bounds.size.x / 2f;
            float offset = _bounds.size.x / 4f;

            children[0] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[1] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[2] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[3] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[4] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[5] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[6] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[7] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);

            for (int i = _vertices.Count - 1; i >= 0; i--)
            {
                int index = GetChildIndex(_vertices[i]);
                children[index].Insert(_vertices[i]);
                _vertices.RemoveAt(i);
            }
        }

        private int GetChildIndex(Vector3 point)
        {
            int index = 0;

            if (point.x > _bounds.center.x)
                index |= 1;

            if (point.y > _bounds.center.y)
                index |= 4;

            if (point.z > _bounds.center.z)
                index |= 2;

            return index;
        }
    }
}