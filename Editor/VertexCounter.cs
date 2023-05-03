using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;
using Hotspot.Editor;
using Rhinox.Lightspeed.Collections;

#if UNITY_EDITOR
using Rhinox.GUIUtils.Editor;
#endif

namespace Rhinox.Hotspot.Editor
{
    public class VertexCounter : CustomEditorWindow
    {
        //should later be exposed
        private static int _MaxVerticesPerCube = 4;
        private static float _minOctreeCubeSize = .1f;

        private float _biggest = 0f;
        private Octree _tree = null;

        private List<KeyValuePair<int, Vector3>> _hotList = new List<KeyValuePair<int, Vector3>>();
        private Vector2 _scrollPos = Vector2.zero;

        //[MenuItem("Tools/Vertex Counter", false, 1)]
        //[ContextMenu("Make Octree")]
        [MenuItem(HotspotWindowHelper.ANALYSIS_PREFIX + "Vertex Density Visualizer", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(VertexCounter));
            win.titleContent = new GUIContent("Vertex Density Visualizer");
        }

        //protected override void Initialize()
        //{
        //base.Initialize();
        //}

        private void VisualizeVertices()
        {
            //FindObjectsOfType is only loaded and active
            //FindObjectsOfTypeAll also includes non-active
            var renderers = Object.FindObjectsOfType<MeshRenderer>();
            var filters = Object.FindObjectsOfType<MeshFilter>();

            if (renderers.Length == 0)
                return;

            Bounds sceneBound = new Bounds();
            foreach (MeshRenderer meshRenderer in renderers)
            {
                sceneBound.Encapsulate(meshRenderer.bounds);
            }

            _biggest = Mathf.Max(Mathf.Max(sceneBound.size.x, sceneBound.size.y), sceneBound.size.z);
            sceneBound.size = new Vector3(_biggest, _biggest, _biggest);

            _tree = new Octree(sceneBound, _MaxVerticesPerCube, _minOctreeCubeSize);

            foreach (MeshFilter meshFilter in filters)
            {
                foreach (var point in meshFilter.sharedMesh.vertices)
                {
                    _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point));
                }
            }

            DrawChildren(_tree);


            //var myList = occurenceList.ToList();
            //myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            _hotList.Sort((pair1, pair2) => pair2.Key.CompareTo(pair1.Key));
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            //GUILayout.BeginArea();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(50f));
            foreach (var item in _hotList)
            {
                GUILayout.Label($"{item.Key}: {item.Value}");
            }
            GUILayout.EndScrollView();
            //GUILayout.EndArea();
            GUILayout.Space(15f);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max vertices per cube:");
            int.TryParse(GUILayout.TextField($"{_MaxVerticesPerCube}", GUILayout.ExpandWidth(true)), out _MaxVerticesPerCube);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Min cube size:");
            float.TryParse(GUILayout.TextField($"{_minOctreeCubeSize}", GUILayout.ExpandWidth(true)), out _minOctreeCubeSize);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Calculate and visualize"))
                VisualizeVertices();

            GUILayout.EndVertical();
        }

        [DrawGizmo(GizmoType.Active)]
        void DrawChildren(Octree tree)
        {
            if (tree.children == null)
            {
                if (tree.VertexCount > _MaxVerticesPerCube)
                {
                    using (new eUtility.GizmoColor(Color.Lerp(Color.white, Color.red, (tree.VertexCount - _MaxVerticesPerCube) / (_MaxVerticesPerCube * 10f))))
                    {
                        //maybe add list of super bad locations that you can click on
                        Handles.Label(tree._bounds.center, $"{tree.VertexCount}");
                        //Gizmos.DrawWireCube(tree._bounds.center, tree._bounds.size);
                        _hotList.Add(new KeyValuePair<int, Vector3>(tree.VertexCount, tree._bounds.center));
                    }
                }

                return;
            }

            foreach (var child in tree.children)
            {
                DrawChildren(child);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_tree == null)
                return;

            DrawChildren(_tree);
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