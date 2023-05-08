﻿using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Editor
{
    public abstract class BaseMeasurableBenchmarkStatistic<T> : BaseBenchmarkStatistic
    {
        private List<T> _samples = new List<T>();
        
        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;
            if (_samples == null)
                _samples = new List<T>();
            else
                _samples.Clear();
            return true;
        }

        protected abstract string GetStatName();

        protected abstract T SampleStatistic();

        public override void Sample()
        {
            _samples.Add(SampleStatistic());
        }
        
        public override void DrawLayout()
        {
            base.DrawLayout();
            
            GUILayout.Label($"{GetStatName()}: {SampleStatistic():0.00}");
        }

        protected abstract float Selector(T value);

        public override BenchmarkResultEntry GetResult()
        {
            return new BenchmarkResultEntry()
            {
                Name = GetStatName(),
                Average = _samples.Average(Selector),
                StdDev = _samples.StdDev(Selector)
            };
        }
    }


    public abstract class BaseMeasurableBenchmarkStatistic : BaseMeasurableBenchmarkStatistic<float>
    {
        protected override float Selector(float value)
        {
            return value;
        }
    }
}