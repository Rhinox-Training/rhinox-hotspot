using System;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    public class RenderedObjectTracker : IDisposable
    {
        private Renderer[] _allRenderersInScene;
        private bool _initialized = false;
        private Renderer[] _filteredRenderers;

        public delegate void VisibleRenderersChangedHandler(RenderedObjectTracker sender, ICollection<Renderer> visibleRenderers);
        public event VisibleRenderersChangedHandler VisibleRenderersChanged;

        public void Initialize()
        {
            if (_initialized)
                return;
            
            RefreshMeshRendererCache();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            _initialized = true;
        }

        public void Terminate()
        {
            if (!_initialized)
                return;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            _initialized = false;
        }

        public void Dispose()
        {
            Terminate();
        }

        public void UpdateForCamera(Camera camera)
        {
            if (camera == null)
                return;

            var filteredRenderers = new List<Renderer>();
            foreach (var r in _allRenderersInScene)
            {
                if (!r.isVisible)
                    continue;

                if (!r.IsWithinFrustum(camera))
                    continue;

                filteredRenderers.Add(r);
            }
            _filteredRenderers = filteredRenderers.ToArray();

            VisibleRenderersChanged?.Invoke(this, _filteredRenderers);
        }
        
        private void RefreshMeshRendererCache()
        {
            _allRenderersInScene = GameObject.FindObjectsOfType<Renderer>();
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RefreshMeshRendererCache();
        }
    }
}