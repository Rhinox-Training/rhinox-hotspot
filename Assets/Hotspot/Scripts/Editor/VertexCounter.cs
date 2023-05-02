using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;
//using System;

namespace Rhinox.Hotspot.Editor
{
    public class VertexCounter : MonoBehaviour
    {
        //should later be exposed
        public int _MaxVerticesPerCube = 4;
        public float _minOctreeCubeSize = .1f;

        private float _biggest = 0f;
        private Octree _tree = null;

        //[MenuItem("Tools/Vertex Counter", false, 1)]
        [ContextMenu("Make Octree")]
        public void ShowVertexCount()
        {
            //FindObjectsOfType is only loaded and active
            //FindObjectsOfTypeAll also includes non-active
            var renderers = Object.FindObjectsOfType<MeshRenderer>();
            var filters = Object.FindObjectsOfType<MeshFilter>();

            //Bounds sceneBound = new Bounds(Vector3.zero, Vector3.zero);
            if (renderers.Length == 0)
                return;

            Bounds sceneBound = renderers[0].bounds;//THIS I HORRIBLE I KNOW

            foreach (MeshRenderer meshRenderer in renderers)
            {
                sceneBound.Encapsulate(meshRenderer.bounds);
            }

            _biggest = Mathf.Max(Mathf.Max(sceneBound.size.x, sceneBound.size.y), sceneBound.size.z);
            //sceneBound.size = new Vector3(_biggest * 1.1f, _biggest * 1.1f, _biggest * 1.1f);
            sceneBound.size = new Vector3(_biggest, _biggest, _biggest);

            _tree = new Octree(sceneBound, _MaxVerticesPerCube, _minOctreeCubeSize);

            //List<Vector3> sceneVertices = new List<Vector3>();
            foreach (MeshFilter meshFilter in filters)
            {
                //sceneVertices.AddRange(meshFilter.sharedMesh.vertices);
                foreach (var point in meshFilter.sharedMesh.vertices)
                {
                    //sceneVertices.Add(meshFilter.gameObject.transform.TransformPoint(point));
                    _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point));
                }

                //foreach (var point in meshFilter.sharedMesh.vertices)
                //{
                //    _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point));
                //    //tree.Insert(point + meshFilter.gameObject.transform.position);
                //}
            }

            //StartCoroutine(AddVertices(sceneVertices));

            //DrawChildren(_tree, 30f);

            Debug.Log(filters.Length);
            Debug.Log("I EXIST \\._./");
        }

        //IEnumerator AddVertices(List<Vector3> vertices)
        //{
        //    foreach (Vector3 v in vertices)
        //    {
        //        Debug.Log("ADD");
        //        _tree.Insert(v);

        //        yield return new WaitForSecondsRealtime(.25f);
        //    }
        //}

        void DrawChildren(Octree tree, int index)
        {
            //DrawBounds(tree._bounds, duration);

            if (tree.children == null)
            {
                if (tree.VertexCount > _MaxVerticesPerCube)
                    Handles.Label(tree._bounds.center, $"{index}: {tree.VertexCount}");

                Gizmos.DrawWireCube(tree._bounds.center, tree._bounds.size);
                return;
            }

            int cnt = 0;
            foreach (var child in tree.children)
            {
                DrawChildren(child, cnt);
                cnt++;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_tree == null)
                return;

            DrawChildren(_tree, 0);
        }

        private void DrawWireCubeWithDebug(Vector3 center, Vector3 size, float duration)
        {
            var halfSize = size * 0.5f;

            Color color = Color.Lerp(Color.red, Color.cyan, size.x / _biggest);

            Vector3 p1 = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p2 = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p3 = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p4 = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);

            // Front top line
            Debug.DrawLine(p1, p2, color, duration);

            // Front bottom line
            Debug.DrawLine(p3, p4, color, duration);

            // Front left line
            Debug.DrawLine(p1, p3, color, duration);

            // Front right line
            Debug.DrawLine(p2, p4, color, duration);


            Vector3 p5 = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            Vector3 p6 = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            Vector3 p7 = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p8 = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);

            // Back top line
            Debug.DrawLine(p5, p6, color, duration);

            // Back bottom line
            Debug.DrawLine(p7, p8, color, duration);

            // Back left line
            Debug.DrawLine(p5, p7, color, duration);

            // Back right line
            Debug.DrawLine(p6, p8, color, duration);

            // Connect front and back lines
            Debug.DrawLine(p1, p5, color, duration);
            Debug.DrawLine(p2, p6, color, duration);
            Debug.DrawLine(p3, p7, color, duration);
            Debug.DrawLine(p4, p8, color, duration);
        }

        void DrawBounds(Bounds b, float duration = 0)
        {
            // bottom
            var p1 = new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z);
            var p2 = new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z);
            var p3 = new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z);
            var p4 = new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z);

            Debug.DrawLine(p1, p2, Color.blue, duration);
            Debug.DrawLine(p2, p3, Color.red, duration);
            Debug.DrawLine(p3, p4, Color.yellow, duration);
            Debug.DrawLine(p4, p1, Color.magenta, duration);

            // top
            var p5 = new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z);
            var p6 = new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z);
            var p7 = new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z);
            var p8 = new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z);

            Debug.DrawLine(p5, p6, Color.blue, duration);
            Debug.DrawLine(p6, p7, Color.red, duration);
            Debug.DrawLine(p7, p8, Color.yellow, duration);
            Debug.DrawLine(p8, p5, Color.magenta, duration);

            // sides
            Debug.DrawLine(p1, p5, Color.white, duration);
            Debug.DrawLine(p2, p6, Color.gray, duration);
            Debug.DrawLine(p3, p7, Color.green, duration);
            Debug.DrawLine(p4, p8, Color.cyan, duration);
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
                Split();
            }
        }

        private void Split()
        {
            children = new Octree[8];

            float childSize = _bounds.size.x / 2f;
            float offset = _bounds.size.x / 4f;

            children[0] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[1] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[2] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[3] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[4] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[5] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[6] = new Octree(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints);
            children[7] = new Octree(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints);

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
            {
                //index |= 4;
                index |= 1;
            }

            if (point.y > _bounds.center.y)
            {
                //index |= 2;
                index |= 4;
            }

            if (point.z > _bounds.center.z)
            {
                //index |= 1;
                index |= 2;
            }

            return index;
        }
    }
}