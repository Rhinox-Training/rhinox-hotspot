﻿using Hotspot.Editor.Utils;
using Hotspot.Utils;
using UnityEngine;

namespace Hotspot.Editor
{
    public struct VertexDensityInfo
    {
        public int VertexCount;
        public float ScreenOccupation;

        public float Density
        {
            get
            {
                if (ScreenOccupation < float.Epsilon)
                    return 0.0f;
                return (float) VertexCount / ScreenOccupation;
            }
        }

        public void MergeWith(VertexDensityInfo other)
        {
            ScreenOccupation += other.ScreenOccupation;
            VertexCount += other.VertexCount;
        }
    }

    public struct MeshInfo
    {
        public Mesh Mesh;
        public Bounds RendererBounds;

        public static bool TryCreate(Renderer renderer, out MeshInfo meshInfo)
        {
            meshInfo = new MeshInfo();
            if (renderer is MeshRenderer meshRenderer)
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    return false;
                meshInfo.Mesh = meshFilter.sharedMesh;
                meshInfo.RendererBounds = meshRenderer.bounds;
                return true;
            }
            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                meshInfo.Mesh = skinnedMeshRenderer.sharedMesh;
                meshInfo.RendererBounds = skinnedMeshRenderer.bounds;
                return true;
            }
            return false;
        }

        public static MeshInfo Create(Renderer renderer)
        {
            if (TryCreate(renderer, out MeshInfo mi))
                return mi;
            return default(MeshInfo);
        }
    }

    
    public static class VertexDensityUtility
    {
        public static bool TryCalculateVertexDensity(Renderer renderer, out VertexDensityInfo info)
        {
            return TryCalculateVertexDensity(renderer, (Camera)null, out info);
        }

        public static bool TryCalculateVertexDensity(Renderer renderer, Camera camera, out VertexDensityInfo info)
        {
            if (renderer == null || !MeshInfo.TryCreate(renderer, out MeshInfo mi))
            {
                info = default(VertexDensityInfo);
                return false;
            }

            var vertexInfo = new VertexDensityInfo()
            {
                VertexCount = EditorPlayModeMeshCache.GetVertexData(mi.Mesh).Length,
                ScreenOccupation = mi.RendererBounds.GetScreenPixels(camera != null ? camera : Camera.main)
            };
            info = vertexInfo;
            return true;
        }

        private static bool TryCalculateVertexDensity(Renderer renderer, CameraSpoof cameraSpoof,
            out VertexDensityInfo info)
        {
            if (renderer == null || !MeshInfo.TryCreate(renderer, out MeshInfo mi))
            {
                info = default(VertexDensityInfo);
                return false;
            }

            var vertexInfo = new VertexDensityInfo()
            {
                VertexCount = EditorPlayModeMeshCache.GetVertexData(mi.Mesh).Length,
                ScreenOccupation = mi.RendererBounds.GetScreenPixels(cameraSpoof)
            };
            info = vertexInfo;
            return true;
        }
        
        public static VertexDensityInfo CalculateVertexDensity(Renderer renderer, Camera camera = null)
        {
            if (TryCalculateVertexDensity(renderer, camera, out VertexDensityInfo info))
                return info;
            return default(VertexDensityInfo);
        }

        public static VertexDensityInfo CalculateVertexDensity(Renderer renderer, CameraSpoof cameraSpoof)
        {
            if (TryCalculateVertexDensity(renderer, cameraSpoof, out VertexDensityInfo info))
                return info;
            return default(VertexDensityInfo);
        }
    }
}