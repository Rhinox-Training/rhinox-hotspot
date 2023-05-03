using System;
using UnityEngine;

namespace Hotspot.Editor
{
    public class RenderedObjectTracker : IDisposable
    {
        private Renderer[] _allRenderersInScene;
        private bool _initialized = false;


        public void Initialize()
        {
            if (_initialized)
                return;
            
            _initialized = true;
        }

        public void Terminate()
        {
            if (!_initialized)
                return;

            _initialized = false;
        }
        

        private void RefreshMeshRendererCache()
        {
            _allRenderersInScene = GameObject.FindObjectsOfType<Renderer>();
        }

        public void Dispose()
        {
            Terminate();
        }
    }
}