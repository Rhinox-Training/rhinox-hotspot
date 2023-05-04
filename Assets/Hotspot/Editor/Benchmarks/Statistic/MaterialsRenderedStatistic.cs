using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public class MaterialsRenderedStatistic : ViewedObjectBenchmarkStatistic
    {
        private Material[] _materials;

        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            _materials = visibleRenderers.SelectMany(x => x.sharedMaterials).ToArray();
        }

        public override void DrawLayout()
        {
            base.DrawLayout();
            
            GUILayout.Label($"Materials: {(_materials != null ? _materials.Length : 0)}");
        }

        public override BenchmarkResultEntry GetResult()
        {
            return new BenchmarkResultEntry()
            {
                Name = "Materials Rendered",
                Average = 0.0f,
                StdDev = 0.0f
            };
        }
    }
}