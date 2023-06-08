using System.Drawing;
using UnityEditor;

namespace Hotspot.Editor
{
    public static class CustomEditorGUILayout
    {
        public static void MinIntField(ref int value, int min, string label = "")
        {
            value = EditorGUILayout.IntField(label, value);
            if (value < min)
                value = min;
        }
    }
}