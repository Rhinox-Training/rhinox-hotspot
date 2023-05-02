using System;
using UnityEngine;

namespace Hotspot.Editor
{
    public interface IBenchmarkStage
    {
        bool Completed { get; }
        bool Failed { get; }
        bool RunStage(ICameraPoseApplier cameraPoseApplier, Action<float> progressCallback = null);
    }
}