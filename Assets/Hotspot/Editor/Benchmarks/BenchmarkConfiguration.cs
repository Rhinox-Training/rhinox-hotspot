using System;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public class BenchmarkConfiguration : ScriptableObject
    {
        public SceneReferenceData Scene;

        [SerializeReference]
        public List<IBenchmarkStage> Entries;
    }
}