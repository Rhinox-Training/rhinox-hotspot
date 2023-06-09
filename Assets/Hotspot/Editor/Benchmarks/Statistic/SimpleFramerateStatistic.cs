﻿using UnityEngine;

namespace Hotspot.Editor
{
    public class SimpleFramerateStatistic : BaseMeasurableBenchmarkStatistic
    {
        protected override string GetStatNameInner()
        {
            return "Framerate";
        }

        protected override float SampleStatistic()
        {
            return 1.0f / Time.smoothDeltaTime;
        }
    }
}