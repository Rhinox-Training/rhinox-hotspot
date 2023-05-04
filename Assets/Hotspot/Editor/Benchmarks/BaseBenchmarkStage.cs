using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hotspot.Editor
{
    public abstract class BaseBenchmarkStage : IBenchmarkStage
    {
        [Title("$_titleString"), PropertyOrder(-10)]
        public string Name;
        private ManagedCoroutine _coroutine;

        private string _titleString => GetType().GetNiceName(false);
        
        [Range(0.0f, 30.0f)]
        public float Duration = 5.0f;

        private ICameraPoseApplier _cameraPoseApplier;

        public bool Completed { get; private set; }
        public bool Failed { get; private set; }

        public bool RunStage(ICameraPoseApplier cameraPoseApplier, Action<float> progress = null)
        {
            if (_coroutine != null)
            {
                PLog.Warn<HotspotLogger>($"Cannot run a stage with an existing coroutine");
                return false;
            }

            Completed = false;
            Failed = false;
            
            _cameraPoseApplier = cameraPoseApplier;
            if (!ValidateInputs())
            {
                PLog.Warn<HotspotLogger>($"Could not run entry {this}, inputs were invalid. Skipping...");
                return false;
            }

            _coroutine = new ManagedCoroutine(RunBenchmarkCoroutine(progress));
            _coroutine.OnFinished += OnFinishedCoroutine;
            _coroutine.OnFailed += OnFailedCoroutine;
            return true;
        }

        public void SetPausedState(bool paused)
        {
            if (_coroutine == null)
                return;

            if (paused)
            {
                if (!_coroutine.Paused)
                    _coroutine.Pause();
            }
            else
            {
                if (_coroutine.Paused)
                    _coroutine.Unpause();
            }
        }

        public void Cancel()
        {
            if (_coroutine == null)
                return;
            
            _coroutine.Stop();
            TriggerFinished(true, "Cancelled by user.");
        }

        protected virtual IEnumerator<float> SplitByIncrement(float duration, float incrementSeconds = 1.0f)
        {
            float time = 0.0f;
            for (time = 0.0f; time < duration; time += incrementSeconds)
            {
                if (time > duration)
                {
                    float remainder = incrementSeconds - (time - duration);
                    yield return remainder;
                }
                else
                    yield return incrementSeconds;
            }
            
        }

        private void OnFinishedCoroutine(bool manual)
        {
            TriggerFinished(false);
        }

        private void OnFailedCoroutine(Exception e)
        {
            TriggerFinished(true, e.ToString());
        }

        public virtual bool ValidateInputs()
        {
            return _cameraPoseApplier != null && Duration > 0.0f && Duration <= 30.0f;
        }

        private void TriggerFinished(bool failed, string reason = null)
        {
            Completed = true;
            Failed = failed;
            if (failed)            
                PLog.Error<HotspotLogger>($"Benchmark stage {this} failed: {reason}");
            OnFinishedStage(failed);
            
            // Clean state
            if (_coroutine != null)
            {
                _coroutine.OnFailed -= OnFailedCoroutine;
                _coroutine.OnFinished -= OnFinishedCoroutine;
                _coroutine = null;
            }
            _cameraPoseApplier = null;
        }

        protected abstract IEnumerator RunBenchmarkCoroutine(Action<float> progressCallback = null);

        protected virtual void ApplyPoseToCamera(Pose pose)
        {
            _cameraPoseApplier.ApplyPoseToCamera(pose);
        }

        public virtual void OnFinishedStage(bool failed)
        {
            if (_cameraPoseApplier != null)
                _cameraPoseApplier.Restore();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}