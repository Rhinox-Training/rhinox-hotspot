using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor.Utils
{
    public enum PlayModeEnterMode
    {
        Normal,
        VertexCached
    }
    
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class EditorApplicationExt
    {
        private static PersistentValue<PlayModeEnterMode> _modeSettings;

        public static bool IsPlaying(PlayModeEnterMode mode)
        {
            if (!Application.isPlaying)
                return false;
            return _modeSettings == mode;
        }

        public delegate void PlayModeStateHandler(PlayModeStateChange change, PlayModeEnterMode mode);
        public static event PlayModeStateHandler PlayModeStateChanged;
        
        static EditorApplicationExt()
        {
            RecreateModeSettingsValue();
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                _modeSettings.Set(PlayModeEnterMode.Normal);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void RecreateModeSettingsValue()
        {
            _modeSettings = PersistentValue<PlayModeEnterMode>.Create($"{nameof(EditorApplicationExt)}.{nameof(_modeSettings)}",
                PlayModeEnterMode.Normal);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    RecreateModeSettingsValue();
                    PlayModeStateChanged?.Invoke(obj, _modeSettings);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    PlayModeStateChanged?.Invoke(obj, _modeSettings);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    PlayModeStateChanged?.Invoke(obj, _modeSettings);
                    _modeSettings.Set(PlayModeEnterMode.Normal);
                    break;
                default:
                    PlayModeStateChanged?.Invoke(obj, PlayModeEnterMode.Normal);
                    break;
            }
        }

        public static void EnterPlayMode(PlayModeEnterMode mode = PlayModeEnterMode.Normal)
        {
            _modeSettings.Set(mode);
            EditorApplication.EnterPlaymode();
        }
    }
}