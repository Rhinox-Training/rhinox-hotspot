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

        private int _cubesInViewCount = 0;
        private float _verticesPerPixel = 0;

        private static VertexOctreeBuilder _octreeBuilder;

        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;

            if (_octreeBuilder == null)
            {
                _octreeBuilder = new VertexOctreeBuilder(_MaxVerticesPerCube, _minOctreeCubeSize);
                _octreeBuilder.CreateOctree();
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
                    return "Vertices per Pixel";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            switch (_mode)
            {
                case VertexDensityMode.Simple:
                    _cubesInViewCount = _octreeBuilder.GetUniqueHotSpotCubes(visibleRenderers);
                    break;
                case VertexDensityMode.Advanced:
                    _verticesPerPixel = _octreeBuilder.GetVerticesPerPixel(visibleRenderers);
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
                    return _verticesPerPixel;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}