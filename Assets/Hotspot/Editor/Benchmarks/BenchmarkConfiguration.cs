using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public class BenchmarkConfiguration : ScriptableObject
    {
        public SceneReferenceData Scene;

        [SerializeReference, AssignableTypeFilter]
        public ICameraPoseApplier PoseApplier;

        [SerializeReference]
        public List<IBenchmarkStage> Entries;

        private ValueDropdownItem[] GetPoseApplierOptions()
        {
            var types = AppDomain.CurrentDomain.GetDefinedTypesOfType(typeof(ICameraPoseApplier));
            return types
                .Select(x => new ValueDropdownItem(x.GetNiceName(false), Activator.CreateInstance(x)))
                .ToArray();
        }
    }
}