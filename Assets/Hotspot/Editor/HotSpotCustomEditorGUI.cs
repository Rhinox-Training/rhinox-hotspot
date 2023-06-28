using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class HotSpotCustomEditorGUI
    {
    // Creates a float field with a label and returns its value.
    public static float LabeledFloatField(string label, float value)
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        float newValue = EditorGUILayout.FloatField(value);
        GUILayout.EndHorizontal();
        return newValue;
    }

    // Creates an integer field with a label and returns its value.
    public static int LabeledIntField(string label, int value)
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        int newValue = EditorGUILayout.IntField(value);
        GUILayout.EndHorizontal();
        return newValue;
    }

    // Creates a toggle field with a label and returns its value.
    public static bool LabeledToggle(string label, bool value)
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        bool newValue = EditorGUILayout.Toggle(value);
        GUILayout.EndHorizontal();
        return newValue;
    }
    }
}