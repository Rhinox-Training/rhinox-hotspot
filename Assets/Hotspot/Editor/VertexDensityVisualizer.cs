using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Rhinox.Utilities.Editor;
using Rhinox.Lightspeed;
using Hotspot.Editor;
using Rhinox.Utilities;

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

        private VertexOctreeBuilder _tree = null;
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

        protected override void OnSelectionChanged()
        {
            if (_denseVertexSpotInfoWindow != null)
                _denseVertexSpotInfoWindow.UpdateTreshold(_MaxVerticesPerCube);
        }

        protected override void OnGUI()
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical();

            _MaxVerticesPerCube.ShowField("Max vertices per cube:", GUILayout.Width(230f));
            if (_MaxVerticesPerCube <= 0)
                _MaxVerticesPerCube.Set(1);

            _minOctreeCubeSize.ShowField("Min cube size:", GUILayout.Width(230f));
            if (_minOctreeCubeSize <= 0)
                _minOctreeCubeSize.Set(0.001f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cube render distance:");
            _cubeViewDistance.Set(GUILayout.HorizontalSlider(_cubeViewDistance, _minCubeDistance, _maxCubeDistance, GUILayout.Width(75f)));
            GUILayout.Space(1f);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (GUILayout.Button("Calculate and visualize"))
            {
                _tree = new VertexOctreeBuilder(_MaxVerticesPerCube, _minOctreeCubeSize, 4f);
                _tree.CreateOctree();
            }

            GUILayout.Space(5f);

            if (GUILayout.Button("Show Dense Spot Info"))
            {
                if (_tree != null)
                {
                    _denseVertexSpotInfoWindow = EditorWindow.GetWindow<DenseVertexSpotWindow>();

                    if (_denseVertexSpotInfoWindow == null)
                        _denseVertexSpotInfoWindow = ScriptableObject.CreateInstance<DenseVertexSpotWindow>();

                    _denseVertexSpotInfoWindow.UpdateTree(_tree);
                    _denseVertexSpotInfoWindow.UpdateTreshold(_MaxVerticesPerCube);

                    DenseVertexSpotWindow.ShowWindow();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawChildren(VertexOctreeBuilder.OctreeNode tree)
        {
            if (tree._children == null)
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

            foreach (var child in tree._children)
            {
                DrawChildren(child);
            }
        }

        protected override void OnSceneGUI(SceneView sceneView)
        {
            base.OnSceneGUI(sceneView);

            if (_tree == null)
                return;

            DrawChildren(_tree.Tree);
        }
    }
}