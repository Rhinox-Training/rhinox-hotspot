using Rhinox.Hotspot.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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


        public override void StartNewRun()
        {
            base.StartNewRun();

            CreateOctree();
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

        protected override string GetStatName()
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


            //throw new NotImplementedException();
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
        public OctreeStatistic[] children { get; private set; }
        public Bounds _bounds { get; private set; }
        public int VertexCount => _vertices.Count;


        private Renderer _renderer = null;
        private List<Vector3> _vertices;
        private readonly float _minSize;
        private readonly int _maxPoints;

        public OctreeStatistic(Bounds bounds, int maxPoints, float minOctSize = 0.1f)
        {
            _minSize = minOctSize;
            _bounds = bounds;
            _maxPoints = maxPoints;
            _vertices = new List<Vector3>();
        }

        public void Insert(Vector3 point, Renderer renderer)
        {
            if (children != null)
            {
                int index = GetChildIndex(point);
                children[index].Insert(point, renderer);
                return;
            }

            _vertices.Add(point);
            _renderer = renderer;

            if (_vertices.Count > _maxPoints && _bounds.size.x > _minSize)
            {
                Split(_minSize);
            }
        }

        private void Split(float minSize)
        {
            children = new OctreeStatistic[8];

            float childSize = _bounds.size.x / 2f;
            float offset = _bounds.size.x / 4f;

            children[0] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[1] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[2] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[3] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y - offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[4] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[5] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z - offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[6] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x - offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);
            children[7] = new OctreeStatistic(new Bounds(new Vector3(_bounds.center.x + offset, _bounds.center.y + offset, _bounds.center.z + offset), new Vector3(childSize, childSize, childSize)), _maxPoints, minSize);

            for (int i = _vertices.Count - 1; i >= 0; i--)
            {
                int index = GetChildIndex(_vertices[i]);
                children[index].Insert(_vertices[i], _renderer);
                _vertices.RemoveAt(i);
            }

            _renderer = null;
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