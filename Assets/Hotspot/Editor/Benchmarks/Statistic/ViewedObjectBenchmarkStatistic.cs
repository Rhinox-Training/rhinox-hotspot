using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotspot.Editor
{
    [Serializable]
    public abstract class ViewedObjectBenchmarkStatistic : BaseBenchmarkStatistic
    {
        private RenderedObjectTracker _objectTracker;

        public void Initialize()
        {
            if (_objectTracker != null)
                return;
            _objectTracker = new RenderedObjectTracker();
            _objectTracker.Initialize();
            _objectTracker.VisibleRenderersChanged += OnVisibleObjectsChanged;
        }

        public void Terminate()
        {
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
            
        }
    }
}