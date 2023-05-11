using Rhinox.Lightspeed;
using System.Linq;
using UnityEngine;

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

    public static float GetScreenPixels(this Bounds bounds, Camera camera)
    {
        var rect = ToScreenSpace(bounds, camera);
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
}
