using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public class MaterialsRenderedStatistic : ViewedObjectBenchmarkStatistic
    {
        [Tooltip("Calls Distinct() on the list, in order to get original materials")]
        public bool Unique = false;
        
        private Material[] _materials;
        
        protected override void HandleObjectsChanged(ICollection<Renderer> visibleRenderers)
        {
            if (Unique)
                _materials = visibleRenderers.SelectMany(x => x.sharedMaterials).Distinct().ToArray();
            else
                _materials = visibleRenderers.SelectMany(x => x.sharedMaterials).ToArray();
        }

        protected override float SampleStatistic()
        {
            return (_materials != null ? _materials.Length : 0);
        }

        protected override string GetStatName()
        {
            return Unique ? "Materials Rendered (Unique)" : "Materials Rendered (Instances)";
        }
    }
}