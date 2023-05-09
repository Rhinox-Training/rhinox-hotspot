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
        protected IBenchmarkStage _currentStage;
        private string _titleString => GetType().GetNiceName(false);
        
        public virtual bool StartNewRun()
        {
            return true;
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

        public void UpdateStage(IBenchmarkStage benchmarkStage)
        {
            if (_currentStage == benchmarkStage)
                return;
            _currentStage = benchmarkStage;
            OnUpdateStage();
        }

        protected virtual void OnUpdateStage()
        {

        }
    }
}