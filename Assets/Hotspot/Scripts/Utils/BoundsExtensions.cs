using Rhinox.Lightspeed;
using System.Linq;
using Hotspot.Utils;
using UnityEngine;
using UnityEngine.Diagnostics;

public static class BoundsExtensions
{
    public static Rect ToScreenSpace(this Bounds bounds, Camera camera)
    {
        var corners = bounds.GetCorners().Select(x => camera.WorldToScreenPoint(x)).ToArray();
        Rect screenbounds = new Rect(corners[0], Vector2.zero);

        for (int idx = 1; idx < corners.Length; ++idx)
        {
            screenbounds = screenbounds.Encapsulate(corners[idx]);
        }

        float oldYmin = screenbounds.yMin;
        screenbounds.yMin = Screen.height - screenbounds.yMax;
        screenbounds.yMax = Screen.height - oldYmin;

        return screenbounds;
    }

    public static Rect ToScreenSpace(Bounds bounds, CameraSpoof cameraSpoof)
    {
        var corners = bounds.GetCorners().Select(x => cameraSpoof.WorldToScreenPoint(x)).ToArray();
        Rect screenbounds = new Rect(corners[0], Vector2.zero);

        for (int idx = 1; idx < corners.Length; ++idx)
        {
            screenbounds = screenbounds.Encapsulate(corners[idx]);
        }

        float oldYmin = screenbounds.yMin;
        screenbounds.yMin = Screen.height - screenbounds.yMax;
        screenbounds.yMax = Screen.height - oldYmin;

        return screenbounds;
    }
    
    public static float GetScreenPixels(this Bounds bounds, Camera camera)
    {
        var rect = ToScreenSpace(bounds, camera);
        return rect.width * rect.height;
    }

    public static float GetScreenPixels(this Bounds bounds, CameraSpoof cameraSpoof)
    {
        var rect = ToScreenSpace(bounds, cameraSpoof);
        return rect.width * rect.height;
    }

    public static Bounds AddMarginToExtends(this Bounds bounds, Vector3 margin)
    {
        bounds.extents += margin;
        return bounds;
    }

    public static Bounds AddMarginToExtends(this Bounds bounds, float margin)
    {
        return bounds.AddMarginToExtends(new Vector3(margin, margin, margin));
    }

    public static bool IsSizeBiggerThan(this Bounds bounds, Vector3 size)
    {
        return bounds.size.x > size.x &&
               bounds.size.y > size.y &&
               bounds.size.z > size.z;
    }

    public static bool IsSizeBiggerThan(this Bounds bounds, float size)
    {
        return bounds.size.x > size &&
               bounds.size.y > size &&
               bounds.size.z > size;
    }
}
