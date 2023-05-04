using System;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable, Title("$_titleString")]
    public abstract class BaseBenchmarkStatistic
    {
        private string _titleString => GetType().GetNiceName(false);
        
        public virtual void StartNewRun()
        {
            
        }
        
        public virtual void CleanUp()
        {
            
        }
        
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