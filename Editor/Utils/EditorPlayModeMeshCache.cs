using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class EditorPlayModeMeshCache
    {
        private static Dictionary<Mesh, Vector3[]> _vertexCache;

        static EditorPlayModeMeshCache()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                if (_vertexCache == null)
                    _vertexCache = new Dictionary<Mesh, Vector3[]>();
                else
                    _vertexCache.Clear();

                var filters = GameObject.FindObjectsOfType<MeshFilter>();
                foreach (var filter in filters)
                {
                    if (filter == null)
                        continue;

                    var sharedMesh = filter.sharedMesh;
                    if (sharedMesh.isReadable)
                        continue;

                    if (_vertexCache.ContainsKey(sharedMesh))
                        continue;

                    _vertexCache.Add(sharedMesh, sharedMesh.vertices.ToArray());
                }

            }
            else if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                if (_vertexCache != null)
                    _vertexCache.Clear();
            }
        }

        public static Vector3[] GetVertexData(MeshFilter filter)
        {
            if (filter == null)
                return Array.Empty<Vector3>();

            if (filter.sharedMesh != null && filter.sharedMesh.isReadable)
                return filter.sharedMesh.vertices;

            if (_vertexCache != null && _vertexCache.ContainsKey(filter.sharedMesh))
                return _vertexCache[filter.sharedMesh];
            return Array.Empty<Vector3>();
        }
    }
}