using System;
using System.Collections;
using Rhinox.Perceptor;
using Rhinox.Utilities;

namespace Hotspot.Editor
{
    public class Benchmark
    {
        public float Progress => _benchmarkProgress;
        public bool IsRunning => _benchmarkRunning;
        
        private bool _benchmarkRunning;
        private float _benchmarkProgress;
        private readonly BenchmarkConfiguration _configuration;
        private IBenchmarkStage _currentStage;

        public event Action BenchmarkTick;

        public Benchmark(BenchmarkConfiguration config)
        {
            _configuration = config;
        }
        

        public void Run()
        {
            if (_benchmarkRunning)
            {
                PLog.Warn<HotspotLogger>($"Benchmark already running, cannot run another one...");
                return;
            }
            _benchmarkProgress = 0.0f;
            _benchmarkRunning = true;
            new ManagedCoroutine(RunBenchmarkCoroutine());
        }

        private IEnumerator RunBenchmarkCoroutine()
        {
            int count = _configuration.Entries.Count;
            int index = 0;
            _currentStage = _configuration.Entries[index];

            float incrementSize = 1.0f / (float)count;
            while (_currentStage != null)
            {
                float stageProgress = 0.0f;
                if (!_currentStage.RunStage(_configuration.PoseApplier, (progress) => { stageProgress = progress; }))
                {
                    PLog.Error<HotspotLogger>($"Benchmark stage {_currentStage} failed to start...");
                    yield break;
                }

                _benchmarkProgress = ((float)index + stageProgress) * incrementSize;
                
                yield return null;
                while (!_currentStage.Completed)
                {
                    _benchmarkProgress = ((float)index + stageProgress) * incrementSize;
                    HandleTick();
                    yield return null;
                }

                if (_currentStage.Failed)
                {
                    PLog.Error<HotspotLogger>($"Benchmark run failed at stage {_currentStage}");
                    yield break;
                }

                if (index < count - 1)
                    _currentStage = _configuration.Entries[++index];
                else
                    _currentStage = null;
            }

            HandleTick();
            _benchmarkProgress = 1.0f;
            _benchmarkRunning = false;
        }

        private void HandleTick()
        {
            if (_configuration.Statistics != null)
            {
                foreach (var stat in _configuration.Statistics)
                {
                    if (stat == null)
                        continue;
                    stat.Sample();
                }
            }
            
            
            BenchmarkTick?.Invoke();
        }
        
        public void DrawLayout()
        {
            if (_configuration.Statistics != null)
            {
                foreach (var stat in _configuration.Statistics)
                {
                    if (stat == null)
                        continue;
                    stat.DrawLayout();
                }
            }
            
            
            BenchmarkTick?.Invoke();
        }
    }
}