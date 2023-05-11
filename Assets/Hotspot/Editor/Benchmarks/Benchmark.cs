using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Rhinox.Perceptor;
using Rhinox.Utilities;
using UnityEngine;

namespace Hotspot.Editor
{
    public class Benchmark
    {
        public float Progress => _benchmarkProgress;
        public bool IsRunning => _benchmarkRunning;
        public bool IsPaused => IsRunning && (_benchmarkCoroutine != null && _benchmarkCoroutine.Paused);

        private Dictionary<IBenchmarkStage, List<BenchmarkResultEntry>> _resultsByStage;
        public Dictionary<IBenchmarkStage, List<BenchmarkResultEntry>> ResultsByStage => _resultsByStage;
        private List<BenchmarkResultEntry> _results;
        public IReadOnlyCollection<BenchmarkResultEntry> Results => _results;


        private bool _benchmarkRunning;
        private float _benchmarkProgress;
        private readonly BenchmarkConfiguration _configuration;
        private IBenchmarkStage _currentStage;
        private ManagedCoroutine _benchmarkCoroutine;


        public delegate void FinishedBenchmarkHandler(Benchmark benchmark, IReadOnlyCollection<BenchmarkResultEntry> results, IReadOnlyDictionary<IBenchmarkStage, List<BenchmarkResultEntry>> resultsByStage);
        public event FinishedBenchmarkHandler Finished;
        public event Action BenchmarkTick;

        public Benchmark(BenchmarkConfiguration config)
        {
            _configuration = config;
            _resultsByStage = new Dictionary<IBenchmarkStage, List<BenchmarkResultEntry>>();
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
            _benchmarkCoroutine = ManagedCoroutine.Begin(RunBenchmarkCoroutine());
            _benchmarkCoroutine.OnFailed += OnFailed;
        }

        private void OnFailed(Exception e)
        {
            PLog.Error<HotspotLogger>(e.ToString());
            _benchmarkRunning = false;
        }

        public void TogglePause()
        {
            if (!IsRunning || _benchmarkCoroutine == null)
            {
                PLog.Warn<HotspotLogger>($"No running benchmark, can't change pause state");
                return;
            }

            bool targetState = !_benchmarkCoroutine.Paused;
            
            if (_currentStage != null)
                _currentStage.SetPausedState(targetState);
            
            if (targetState)
                _benchmarkCoroutine.Pause();
            else
                _benchmarkCoroutine.Unpause();
        }

        public void Cancel()
        {
            if (!IsRunning || _benchmarkCoroutine == null)
            {
                PLog.Warn<HotspotLogger>($"No running benchmark, cannot be cancelled...");
                return;
            }

            if (_currentStage != null)
                _currentStage.Cancel();
            
            _benchmarkCoroutine.Stop();
            _benchmarkCoroutine = null;
            _benchmarkProgress = 0.0f;
            _benchmarkRunning = false;
        }

        private IEnumerator RunBenchmarkCoroutine()
        {
            int count = _configuration.Entries.Count;
            int index = 0;
            _currentStage = _configuration.Entries[index];
            
            // Initialize statistics
            var stats = new List<BaseBenchmarkStatistic>();
            if (_configuration.Statistics != null)
            {
                foreach (var stat in _configuration.Statistics)
                {
                    if (stat == null)
                        continue;
                    if (!stat.StartNewRun())
                    {
                        PLog.Warn<HotspotLogger>($"Failed to start stat '{stat}'...");
                        continue;
                    }
                    else
                    {
                        stats.Add(stat);
                    }
                }
            }

            float incrementSize = 1.0f / (float)count;
            bool finished = false;
            while (!finished)
            {
                float stageProgress = 0.0f;
                if (!_currentStage.RunStage(_configuration.PoseApplier, (progress) => { stageProgress = progress; }))
                {
                    PLog.Error<HotspotLogger>($"Benchmark stage {_currentStage} failed to start...");
                    yield break;
                }

                _benchmarkProgress = ((float)index + stageProgress) * incrementSize;
                HandleTick(_currentStage, stats);
                yield return null;
                while (!_currentStage.Completed)
                {
                    _benchmarkProgress = ((float)index + stageProgress) * incrementSize;
                    HandleTick(_currentStage, stats);
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
                {
                    finished = true;
                }
            }

            HandleTick(_currentStage, stats);
            _benchmarkProgress = 1.0f;
            _benchmarkRunning = false;

            
            // Cleanup statistics
            foreach (var stat in stats)
            {
                if (stat == null)
                    continue;
                stat.CleanUp();
            }

            HandleFinish(stats);
        }

        private void HandleTick(IBenchmarkStage benchmarkStage, ICollection<BaseBenchmarkStatistic> stats)
        {
            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    if (stat == null)
                        continue;
                    stat.UpdateStage(benchmarkStage);
                    stat.Sample();
                }
            }
            
            
            BenchmarkTick?.Invoke();
        }

        private void HandleFinish(ICollection<BaseBenchmarkStatistic> stats)
        {
            if (_resultsByStage == null)
                _resultsByStage = new Dictionary<IBenchmarkStage, List<BenchmarkResultEntry>>();
            if (_results == null)
                _results = new List<BenchmarkResultEntry>();
            
            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    if (stat == null)
                        continue;
                    var result = stat.GetResultsPerStage();
                    if (result != null)
                    {
                        foreach (var stage in result.Keys)
                        {
                            if (!_resultsByStage.ContainsKey(stage))
                                _resultsByStage.Add(stage, new List<BenchmarkResultEntry>());

                            _resultsByStage[stage].Add(result[stage]);
                        }
                    }

                    var summarizedResult = stat.GetResult();
                    if (summarizedResult != null)
                    {
                        _results.Add(summarizedResult);
                    }
                }
            }

            Finished?.Invoke(this, _results, _resultsByStage);
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
        }
    }
}