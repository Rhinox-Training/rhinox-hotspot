using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class OcclusionPortalExtensions
    {
        private const string k_CenterPath = "m_Center";
        private const string k_SizePath = "m_Size";

        public static void UpdateBounds(this OcclusionPortal portal, Vector3 center, Vector3 size)
        {
            var so = new SerializedObject(portal);
            var centerProperty = so.FindProperty(k_CenterPath);
            centerProperty.SetValue(center);
            var sizeProperty = so.FindProperty(k_SizePath);
            sizeProperty.SetValue(size);
            if (so.hasModifiedProperties)
                so.ApplyModifiedProperties();
        }

        public static void UpdateBounds(this OcclusionPortal portal, Bounds bounds)
        {
            UpdateBounds(portal, bounds.center, bounds.size);
        }

        public static Bounds GetBounds(this OcclusionPortal portal)
        {
            var so = new SerializedObject(portal);
            var centerProperty = so.FindProperty(k_CenterPath);
            Vector3 center = (Vector3)centerProperty.GetValue();
            var sizeProperty = so.FindProperty(k_SizePath);
            Vector3 size = (Vector3)sizeProperty.GetValue();
            return new Bounds(center, size);
        }
    }
}