#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class CustomDebugPanelWindow : EditorWindow
{
    [MenuItem("Window/Custom Debug Panel")]
    public static void ShowWindow()
    {
        GetWindow<CustomDebugPanelWindow>("Custom Debug Panel");
    }

    private void OnEnable()
    {
        // Create a root container for the custom controls
        var root = rootVisualElement;

        // Create a checkbox to toggle the main directional light
        var checkbox = new Toggle("Enable main directional light");
        checkbox.RegisterValueChangedCallback(OnCheckboxValueChanged);
        root.Add(checkbox);

        // Add your custom controls here
    }

    private void OnCheckboxValueChanged(ChangeEvent<bool> evt)
    {
        Light mainLight = FindObjectsOfType<Light>().Where(x => x.type == LightType.Directional).FirstOrDefault();
        mainLight.enabled = evt.newValue;
    }
}
#endif