using System.Collections;
using System.Collections.Generic;
using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

public abstract class AnalysisInfo
{
    private bool _isUnfolded = false;

    public void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawHeaderGUI();
        DrawObjectField();
        EditorGUILayout.EndHorizontal();
        if (_isUnfolded)
            DrawUnfoldedGUI();
    }

    private void DrawHeaderGUI()
    {
        string iconName = _isUnfolded ? "Fa_AngleUp" : "Fa_AngleDown";
        if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon(iconName)))
        {
            // Toggle open status
            _isUnfolded = !_isUnfolded;
        }
    }

    protected abstract void DrawObjectField();

    protected abstract void DrawUnfoldedGUI();
}