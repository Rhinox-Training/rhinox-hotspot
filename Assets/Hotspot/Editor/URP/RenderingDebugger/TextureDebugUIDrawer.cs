using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hotspot.Editor
{
    [DebugUIDrawer(typeof(TextureDebugUIField))]
    public class TextureDebugUIDrawer : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<TextureDebugUIField>(widget);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            var value = EditorGUI.ObjectField(rect, w.displayName, w.GetValue(), typeof(Texture2D), false) as Texture2D;
            
            if (EditorGUI.EndChangeCheck())
                Apply(w, state, value);

            return true;

        }
    }
}