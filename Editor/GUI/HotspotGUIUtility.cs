using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class HotspotGUIUtility
    {
        public static Rect IndentedRect(Rect source, float indentMultiplier = 15f)
        {
            float indent = EditorGUI.indentLevel * indentMultiplier;
            return new Rect(source.x + indent, source.y, source.width - (2 * indent), source.height);
        }
        
    }
}