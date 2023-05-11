using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Editor
{
    public abstract class BaseMeasurableBenchmarkStatistic<T> : BaseBenchmarkStatistic
    {
        private Dictionary<IBenchmarkStage, List<T>> _samplesByStage;
        
        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;
            if (_samplesByStage == null)
                _samplesByStage = new Dictionary<IBenchmarkStage, List<T>>();
            else
                _samplesByStage.Clear();
            return true;
        }

        protected abstract string GetStatName();

        protected abstract T SampleStatistic();

        public override void Sample()
        {
            _samplesByStage[_currentStage].Add(SampleStatistic());
        }
        
        public override void DrawLayout()
        {
            base.DrawLayout();
            
            GUILayout.Label($"{GetStatName()}: {Selector(SampleStatistic()):0.00}");
        }

        protected abstract float Selector(T value);

        public override BenchmarkResultEntry GetResult()
        {
            return new BenchmarkResultEntry()
            {
                Name = GetStatName(),
                Average = _samplesByStage.SelectMany(x => x.Value).Average(Selector),
                StdDev = _samplesByStage.SelectMany(x => x.Value).StdDev(Selector)
            };
        }
        
        public override Dictionary<IBenchmarkStage, BenchmarkResultEntry> GetResultsPerStage()
        {
            var dict = new Dictionary<IBenchmarkStage, BenchmarkResultEntry>();
            foreach (var stage in _samplesByStage.Keys)
            {
                dict.Add(stage, new BenchmarkResultEntry()
                {
                    Name = GetStatName(),
                    Average = _samplesByStage[stage].Average(Selector),
                    StdDev = _samplesByStage[stage].StdDev(Selector)
                });
            }
            return dict;
        }

        protected override void OnUpdateStage()
        {
            base.OnUpdateStage();
            if (_currentStage == null)
                return;
            if (!_samplesByStage.ContainsKey(_currentStage))
                _samplesByStage.Add(_currentStage, new List<T>());
        }
    }


    public abstract class BaseMeasurableBenchmarkStatistic : BaseMeasurableBenchmarkStatistic<float>
    {
        public virtual UnitConverter Converter => UnitConverter.DefaultConverter;

        protected abstract string GetStatNameInner();

        protected sealed override string GetStatName()
        {
            if (Converter != null && !string.IsNullOrWhiteSpace(Converter.Unit))
                return $"{GetStatNameInner()} [{Converter.Unit}]";
            return GetStatNameInner();
        }

        protected override float Selector(float value)
        {
            if (Converter != null)
                return value * Converter.ConversionValue;
            return value;
        }
    }

    public class UnitConverter
    {
        public string Unit;
        public float ConversionValue;

        public UnitConverter(string unit, float conversion = 1.0f)
        {
            Unit = unit;
            ConversionValue = conversion;
        }

        public static readonly UnitConverter DefaultConverter = new UnitConverter("");
        
        public static UnitConverter MegabyteConverter => new UnitConverter("MB", 1e-6f);
        public static UnitConverter KilobyteConverter => new UnitConverter("KB", 1e-3f);
        public static UnitConverter ThreadMillisecondConverter => new UnitConverter("ms", 1e-6f);
    }
}