using System.Globalization;
using Rhinox.GUIUtils.Editor;
using Rhinox.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public class LODDetailsOverlayWindow : CustomSceneOverlayWindow<LODDetailsOverlayWindow>
    {
        private const string MENU_ITEM_PATH = WindowHelper.ToolsPrefix + "Show LOD Details";
        protected override string Name => "LOD Details";
        private bool _requiresRefresh = true;
        protected override string GetMenuPath() => MENU_ITEM_PATH;

        private LODGroup _currentLODGroup;
        private LOD[] _lods;
        private Camera _camera;

        [MenuItem(MENU_ITEM_PATH, false, -199)]
        public static void SetupWindow() => Window.Setup();

        [MenuItem(MENU_ITEM_PATH, true)]
        public static bool SetupValidateWindow() => Window.HandleValidateWindow();

        protected override void OnBeforeDraw()
        {
            base.OnBeforeDraw();
            if (_requiresRefresh)
                RefreshInfo();
        }

        protected override void OnSelectionChanged()
        {
            _requiresRefresh = true;
        }

        private void RefreshInfo()
        {
            _currentLODGroup = null;

            if (_camera == null)
            {
                if (!HotSpotUtils.TryGetMainCamera(out _camera))
                    return;
            }

            if (Selection.gameObjects.Length != 1)
                return;

            _currentLODGroup = Selection.gameObjects[0].GetComponent<LODGroup>();
            if (_currentLODGroup == null)
                return;
            _lods = _currentLODGroup.GetLODs();
        }

        protected override void OnGUI()
        {
            if (_currentLODGroup == null)
            {
                GUILayout.Label("Please select 1 object containing a LOD group.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current height percentage: ");
            GUILayout.Label(_currentLODGroup.CalculateCurrentTransitionPercentage(_camera).ToString("###"));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current LOD Index: ");
            GUILayout.Label(_currentLODGroup.GetCurrentLODIndex(_camera).ToString());
            GUILayout.EndHorizontal();

            GUILayout.Label("LOD height pixel densities", EditorStyles.boldLabel);
            for (int i = 0; i < _lods.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"\tLOD {i}: ");
                GUILayout.Label(_currentLODGroup.GetVertexHeightDensityForLOD(i, _camera).ToString("0##.000"));
                GUILayout.EndHorizontal();
            }
        }
    }
}