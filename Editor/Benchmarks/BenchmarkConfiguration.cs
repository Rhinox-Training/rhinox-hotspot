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

        [AssignableTypeFilter(typeof(ICameraPoseApplier)), SerializeField, OnValueChanged(nameof(OnPoseApplierTypeChanged))]
        public SerializableType PoseApplierType;
        
        [SerializeReference, HideInInspector]
        public ICameraPoseApplier PoseApplier;

        [SerializeReference]
        public List<IBenchmarkStage> Entries;

        private void OnPoseApplierTypeChanged()
        {
            if (PoseApplierType == null)
            {
                PoseApplier = null;
                return;
            }
            
            PoseApplier = Activator.CreateInstance(PoseApplierType) as ICameraPoseApplier;
        }
    }
}