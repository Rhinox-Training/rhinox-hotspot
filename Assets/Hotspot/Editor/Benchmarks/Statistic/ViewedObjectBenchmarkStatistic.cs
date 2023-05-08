using System;
using System.Collections.Generic;
using Codice.Client.ChangeTrackerService;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public abstract class ViewedObjectBenchmarkStatistic : BaseMeasurableBenchmarkStatistic
    {
        private static RenderedObjectTracker _objectTracker;

        public override bool StartNewRun()
        {
            if (!base.StartNewRun())
                return false;
            
            if (_objectTracker == null)
            {
                _objectTracker = new RenderedObjectTracker();
                _objectTracker.Initialize();
                _objectTracker.VisibleRenderersChanged += OnVisibleObjectsChanged;
            }
            return true;
        }

        public override void CleanUp()
        {
            base.CleanUp();
            if (_objectTracker != null)
            {
                _objectTracker.VisibleRenderersChanged -= OnVisibleObjectsChanged;
                _objectTracker.Terminate();
                _objectTracker = null;
            }
        }

        private void OnVisibleObjectsChanged(RenderedObjectTracker sender, ICollection<Renderer> visibleRenderers)
        {
            HandleObjectsChanged(visibleRenderers ?? Array.Empty<Renderer>());
        }

        protected abstract void HandleObjectsChanged(ICollection<Renderer> visibleRenderers);

        public override void Sample()
        {
            if (_objectTracker != null && Camera.main != null)
                _objectTracker.UpdateForCamera(Camera.main);
            base.Sample();
        }
    }
}