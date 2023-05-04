using System;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public abstract class BaseBenchmarkStatistic
    {
        public abstract void Sample();

        public virtual void DrawLayout()
        {
            
        }

        public virtual BenchmarkResultEntry GetResult()
        {
            return null;
        }
    }
}