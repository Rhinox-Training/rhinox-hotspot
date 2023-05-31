using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class CustomDebugPanel : MonoBehaviour
{
    static bool lightEnabled = true;

    void OnEnable()
    {
        // Create a list of widgets
        var widgetList = new List<DebugUI.Widget>();
        // Add a checkbox widget to the list of widgets
        widgetList.AddRange(new DebugUI.Widget[]
        {
            new DebugUI.BoolField
            {
                displayName = "Enable main directional light",
                tooltip = "Enable or disable the main directional light",
                getter = () => lightEnabled,
                // When the widget value is changed, change the value of lightEnabled
                setter = value => lightEnabled = value,
                // Run a custom function to enable or disable the main directional light based on the widget value
                onValueChanged = DisplaySunChanged
            },
        });
        // Create a new panel (tab) in the Rendering Debugger
        var panel = DebugManager.instance.GetPanel("My Custom Panel", createIfNull: true);
        // Add the widgets to the panel
        panel.children.Add(widgetList.ToArray());
    }

    // Remove the custom panel if the GameObject is disabled
    void OnDisable()
    {
        DebugManager.instance.RemovePanel("My Custom Panel");
    }

    // Enable or disable the main directional light based on the widget value
    void DisplaySunChanged(DebugUI.Field<bool> field, bool displaySun)
    {
        Light sun = FindObjectsOfType<Light>().Where(x => x.type == LightType.Directional).FirstOrDefault();
        if (sun)
            sun.enabled = displaySun;
    }
}