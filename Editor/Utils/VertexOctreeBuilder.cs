using Hotspot.Editor;
using Rhinox.Lightspeed;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexOctreeBuilder
{
    public int _MaxVerticesPerCube = 500;
    public float _minOctreeCubeSize = 1f;

    private OctreeNode _tree = null;

    public OctreeNode Tree => _tree;

    public VertexOctreeBuilder(int maxVerticesPerCube, float minOctreeCubeSize)
    {
        _MaxVerticesPerCube = maxVerticesPerCube;
        _minOctreeCubeSize = minOctreeCubeSize;
    }

    public bool CreateOctree()
    {
        var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
        var filters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();

        if (renderers.Length == 0 || filters.Length == 0)
            return false;

        Bounds sceneBound = new Bounds();
        foreach (MeshRenderer meshRenderer in renderers)
        {
            if (!LODRendererCache.IsLOD(meshRenderer))
                sceneBound.Encapsulate(meshRenderer.bounds);
        }

        float biggestSide = Mathf.Max(Mathf.Max(sceneBound.size.x, sceneBound.size.y), sceneBound.size.z);
        sceneBound.size = new Vector3(biggestSide, biggestSide, biggestSide);

        _tree = new OctreeNode(sceneBound, _MaxVerticesPerCube, _minOctreeCubeSize);

        foreach (MeshFilter meshFilter in filters)
        {
            //first check if the meshfilters renderer is and LOD, if so, discard and goto next
            if (!meshFilter.gameObject.TryGetComponent<Renderer>(out var renderer))
                continue;

            if (LODRendererCache.IsLOD(renderer))
                continue;

            foreach (var point in EditorPlayModeMeshCache.GetVertexData(meshFilter))
            {
                _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point), renderer);
            }
        }

        return _tree.VertexCount > 0 || _tree._children != null;
    }

    public int GetUniqueHotSpotCubes(ICollection<Renderer> visibleRenderers)
    {
        if (_tree == null)
            return 0;

        return _tree.GetUniqueHotSpotCubes(visibleRenderers);
    }

    internal void Terminate()
    {
        if (_tree != null)
        {
            _tree.Cleanup();
            _tree = null;
        }
    }

    public class OctreeNode
    {
        public OctreeNode[] _children { get; private set; }
        public Bounds _bounds { get; private set; }
        public int VertexCount => _vertices.Count;


        private HashSet<Renderer> _renderers = null;
        private List<Vector3> _vertices;
        private readonly float _minSize;
        private readonly int _maxPoints;

        public OctreeNode(Bounds bounds, int maxPoints, float minOctSize = 0.1f)
        {
            _minSize = minOctSize;
            _bounds = bounds;
            _maxPoints = maxPoints;
            _vertices = new List<Vector3>();
            _renderers = new HashSet<Renderer>();
        }

        //public List<Bounds>
        public int GetUniqueHotSpotCubes(in ICollection<Renderer> renderList)
        {
            int count = 0;

            if (_children != null)
            {
                foreach (var child in _children)
                {
                    count += child.GetUniqueHotSpotCubes(renderList);
                }
            }
            else
            {
                //if the spot is actually a hotspot and
                //if the cube's render list contains one of the renderers from the visible renderer list,
                //then this cube should count towards the visible cube count.
                //convert bool to int because, true is 1 and false is 0.
                return Convert.ToInt32(_vertices.Count > _maxPoints && _renderers.ContainsAny(renderList));
            }

            return count;
        }

        public void Insert(Vector3 point, Renderer renderer = null)
        {
            if (_children != null)
            {
                int index = GetChildIndex(point);
                _children[index].Insert(point, renderer);
                return;
            }

            _vertices.Add(point);

            if (renderer != null)
                _renderers.Add(renderer);

            //_renderers.AddUnique = renderer;//redundant function?

            if (_vertices.Count > _maxPoints && _bounds.size.x > _minSize)
                Split(_minSize);
        }

        private void Split(float minSize)
        {
            _children = new OctreeNode[8];

            float childSize = _bounds.size.x / 2f;
            float offset = _bounds.size.x / 4f;

            _children[0] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[1] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[2] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[3] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[4] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[5] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[6] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[7] = new OctreeNode(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);

            for (int i = _vertices.Count - 1; i >= 0; i--)
            {
                int index = GetChildIndex(_vertices[i]);
                _children[index].Insert(_vertices[i]);
                _children[index].AddRenderers(_renderers);
                _vertices.RemoveAt(i);
            }

            _renderers.Clear();// = null;
        }

        public void Cleanup()
        {
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.Cleanup();
                }
            }

            _renderers?.Clear();
            _vertices?.Clear();
        }

        private void AddRenderers(ICollection<Renderer> renderers)
        {
            _renderers.AddRange(renderers);
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
