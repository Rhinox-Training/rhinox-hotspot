using Rhinox.Hotspot.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Rhinox.Lightspeed;

namespace Hotspot.Editor
{
    public enum VertexDensityMode
    {
        Simple,
        Advanced
    }

    [Serializable]
    public class VertexDensityStatistic : ViewedObjectBenchmarkStatistic
    {
        public VertexDensityMode _mode = VertexDensityMode.Simple;

        public int _MaxVerticesPerCube = 500;
        public float _minOctreeCubeSize = 1f;

        private OctreeStatistic _tree = null;

        private int _cubesInViewCount = 0;


        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;

            CreateOctree();
            return true;
        }

        private void CreateOctree()
        {
            var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            var filters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();

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

            _tree = new OctreeStatistic(sceneBound, _MaxVerticesPerCube, _minOctreeCubeSize);

            foreach (MeshFilter meshFilter in filters)
            {
                //first check if the meshfilters renderer is and LOD, if so, discard and goto next
                if (!meshFilter.gameObject.TryGetComponent<Renderer>(out var renderer))
                    continue;

                if (LODRendererCache.IsLOD(renderer))
                    continue;

                //List<Vector3> listo = new List<Vector3>();
                //meshFilter.mesh.GetVertices(listo);

                foreach (var point in /*meshFilter.sharedMesh.vertices*/EditorPlayModeMeshCache.GetVertexData(meshFilter))
                {
                    _tree.Insert(meshFilter.gameObject.transform.TransformPoint(point), renderer);
                }
            }
        }

        //cleanup
        public override void CleanUp()
        {
            base.CleanUp();

        }

        protected override string GetStatNameInner()
        {
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    return "Amount of Hotspots in view";
                case VertexDensityMode.Advanced:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            _cubesInViewCount = _tree.GetUniqueHotSpotCubes(visibleRenderers);
        }

        protected override float SampleStatistic()
        {
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    return _cubesInViewCount;
                case VertexDensityMode.Advanced:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class OctreeStatistic
    {
        public OctreeStatistic[] _children { get; private set; }
        public Bounds _bounds { get; private set; }
        public int VertexCount => _vertices.Count;


        private HashSet<Renderer> _renderers = null;
        private List<Vector3> _vertices;
        private readonly float _minSize;
        private readonly int _maxPoints;

        public OctreeStatistic(Bounds bounds, int maxPoints, float minOctSize = 0.1f)
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
            _children = new OctreeStatistic[8];

            float childSize = _bounds.size.x / 2f;
            float offset = _bounds.size.x / 4f;

            _children[0] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[1] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[2] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[3] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[4] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[5] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[6] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            _children[7] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);

            for (int i = _vertices.Count - 1; i >= 0; i--)
            {
                int index = GetChildIndex(_vertices[i]);
                _children[index].Insert(_vertices[i]);
                _children[index].AddRenderers(_renderers);
                _vertices.RemoveAt(i);
            }

            _renderers.Clear();// = null;
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