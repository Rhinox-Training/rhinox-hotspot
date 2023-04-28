using System;
using System.Collections;
using Hotspot.Scripts;
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
        private Camera _camera;
        private ManagedCoroutine _coroutine;

        private string _titleString => GetType().GetNiceName(false);
        
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }

        public bool RunStage(Camera camera)
        {
            if (_coroutine != null)
            {
                PLog.Warn<HotspotLogger>($"Cannot run a stage with an existing coroutine");
                return false;
            }
            
            _camera = camera;
            if (!ValidateInputs())
            {
                PLog.Warn<HotspotLogger>($"Could not run entry {this}, inputs were invalid. Skipping...");
                return false;
            }

            _coroutine = new ManagedCoroutine(RunBenchmarkCoroutine());
            _coroutine.OnFinished += OnFinishedCoroutine;
            _coroutine.OnFailed += OnFailedCoroutine;
            return true;
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
            return _camera != null;
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
            _camera = null;
        }

        protected abstract IEnumerator RunBenchmarkCoroutine();

        protected virtual void ApplyPoseToCamera(Pose pose)
        {
            _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public virtual void OnFinishedStage(bool failed)
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}