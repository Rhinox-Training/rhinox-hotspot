using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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

        [ShowIf(nameof(_dummyAdvancedModeBool))]
        public float _vertsPerPixelThreshold = 4f;

        private bool _dummyAdvancedModeBool = true;

        private int _cubesInViewCount = 0;
        private float _hotVertsPerPixelCount = 0;

        private static VertexOctreeBuilder _octreeBuilder;
        private HashSet<Mesh> _meshList;

        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;

            if (_octreeBuilder == null)
            {
                _octreeBuilder = new VertexOctreeBuilder(_MaxVerticesPerCube, _minOctreeCubeSize, _vertsPerPixelThreshold);
                _octreeBuilder.CreateOctree(true);
            }

            return true;
        }

        //cleanup
        public override void CleanUp()
        {
            base.CleanUp();

            if (_octreeBuilder != null)
            {
                _octreeBuilder.Terminate();
                _octreeBuilder = null;
            }
        }

        protected override string GetStatNameInner()
        {
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    return "Amount of Hotspots in view";
                case VertexDensityMode.Advanced:
                    return "amount of Hotspots (vert/pixel) in view";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            if (_meshList == null)
                _meshList = new HashSet<Mesh>();
            else
                _meshList.Clear();

            foreach (var renderer in visibleRenderers)
            {
                if (renderer == null)
                    continue;
                
                if (renderer is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        _meshList.Add(filter.sharedMesh);
                }
                else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    if (skinnedMeshRenderer.sharedMesh != null)
                        _meshList.Add(skinnedMeshRenderer.sharedMesh);
                }
            }
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    _cubesInViewCount = _octreeBuilder.GetUniqueHotSpotCubes(_meshList);
                    break;
                case VertexDensityMode.Advanced:
                    _hotVertsPerPixelCount = _octreeBuilder.GetVerticesPerPixel(_meshList);
                    break;
            }
        }

        protected override float SampleStatistic()
        {
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    return _cubesInViewCount;
                case VertexDensityMode.Advanced:
                    return _hotVertsPerPixelCount;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}