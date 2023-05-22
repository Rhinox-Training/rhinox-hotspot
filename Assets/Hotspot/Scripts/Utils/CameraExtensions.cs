using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Hotspot.Utils
{
    public static class CameraExtensions
    {
        public static float CalculateScreenHeightPercentage(this Camera cam, IEnumerable<MeshInfo> meshInfos)
        {
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (var meshInfo in meshInfos)
            {
                foreach (Vector3 vertex in meshInfo.Mesh.vertices)
                {
                    //Transform the mesh vertex into world space
                    float screenY = Mathf.Clamp01(cam.WorldToViewportPoint(vertex).y);
                    minY = Mathf.Min(minY, screenY);
                    maxY = Mathf.Max(maxY, screenY);
                }
            }

            // Calculate the height in screen space
            float height = maxY - minY;

            return height;
        }

        public static float CalculateScreenHeightPercentage(this Camera cam, IEnumerable<Renderer> renderers)
        {
            var bounds = renderers.Select(x => x.bounds).ToArray().Combine();
            return cam.CalculateScreenHeightPercentage(bounds);
        }

        public static float CalculateScreenHeightPercentage(this Camera cam, GameObject gameObject)
        {
            return cam.CalculateScreenHeightPercentage(gameObject.GetObjectBounds());
        }

        public static float CalculateScreenHeightPercentage(this Camera cam, Bounds bounds)
        {
            // Get the corners of the bounding box
            var corners = bounds.GetCorners();

            // Transform each corner into viewport space and get its y value
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            foreach (Vector3 corner in corners)
            {
                float screenY = Mathf.Clamp01(cam.WorldToViewportPoint(corner).y);
                minY = Mathf.Min(minY, screenY);
                maxY = Mathf.Max(maxY, screenY);
            }

            // Calculate the height in viewport space
            float height = maxY - minY;

            return height;
        }
    }
}