using System;
using System.Collections.Generic;
using System.Linq;
using Hotspot.Editor.Utils;
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
            EditorApplicationExt.PlayModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj, PlayModeEnterMode mode)
        {
            if (mode == PlayModeEnterMode.Normal)
                return;
            
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
            if (filter == null || filter.sharedMesh == null)
                return Array.Empty<Vector3>();
            
            var mesh = filter.sharedMesh;
            return GetVertexData(mesh);
        }
        
        public static Vector3[] GetVertexData(Mesh mesh)
        {
            if (mesh == null)
                return Array.Empty<Vector3>();

            if (mesh.isReadable || !Application.isPlaying)
                return mesh.vertices;
            
            if (_vertexCache != null && _vertexCache.ContainsKey(mesh))
                return _vertexCache[mesh];
            return Array.Empty<Vector3>();
        }
    }
}