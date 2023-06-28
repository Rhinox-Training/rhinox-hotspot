using System.Collections.Generic;
using Hotspot.Scripts.Utils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public class GradientTextureCreatorWindow : CustomEditorWindow
    {
        private List<GradientColorKey> _gradientStops;
        private PageableReorderableList _pageableList;
        private Texture2D _previewTexture = null;
        private bool _sortStopsByTime = false;

        private const int PREVIEW_TEXTURE_HEIGHT = 256;

        //---------------------------------------------------------------------
        // TEXTURE SAVE SETTINGS
        //---------------------------------------------------------------------
        private int _textureWidth = 256;
        private int _textureHeight = 256;
        private bool _overwriteFile = true;
        private string _path = string.Empty;
        private TextureFactory.FileSaveFormat _fileFormat = TextureFactory.FileSaveFormat.PNG;


        private Vector2 _scrollPos = Vector2.zero;

        // Comparer object for gradient color keys
        private static Comparer<GradientColorKey> _gradientKeyCmp =
            Comparer<GradientColorKey>.Create((s1, s2) => s1.time.CompareTo(s2.time));


        [MenuItem(HotspotWindowHelper.TOOLS_PREFIX + "Gradient Texture Creator", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(GradientTextureCreatorWindow));
            win.titleContent = new GUIContent("Create Gradient Texture");
        }

        protected override void Initialize()
        {
            _path = Application.dataPath;

            // Initialize gradient stops if null
            if (_gradientStops == null)
            {
                _gradientStops = new List<GradientColorKey>
                {
                    new GradientColorKey(Color.black, 0),
                    new GradientColorKey(Color.white, 1)
                };

                _previewTexture = CreateTexture(_textureWidth, PREVIEW_TEXTURE_HEIGHT);
            }

            // Initialize pageable list if null
            _pageableList ??= new PageableReorderableList(_gradientStops)
            {
                drawElementCallback = OnDrawElement,
                elementHeight = CalculateElementHeight()
            };
        }

        protected override void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Begin monitoring changes in GUI
            EditorGUI.BeginChangeCheck();

            DrawHeaderGUI();

            CustomEditorGUI.Title("Gradient stops");
            _pageableList.DoLayoutList(GUIContent.none);

            // If GUI has been changed
            if (EditorGUI.EndChangeCheck())
            {
                if (_gradientStops.Count > 0)
                {
                    if (_sortStopsByTime)
                        _gradientStops.Sort(_gradientKeyCmp);

                    _previewTexture = CreateTexture(_textureWidth, PREVIEW_TEXTURE_HEIGHT);
                }
                else
                {
                    _previewTexture = null;
                    return;
                }
            }

            // Preview texture
            DrawPreviewTextureGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawSaveGUI();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the Window title and settings
        /// </summary>
        private void DrawHeaderGUI()
        {
            // Draw pageable list and title
            CustomEditorGUI.Title("Create Custom Gradient Texture", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            CustomEditorGUI.Title("Settings");
            using (new eUtility.IndentedLayout())
            {
                CustomEditorGUILayout.MinIntField(ref _textureWidth, 1, "Texture Width");
                CustomEditorGUILayout.MinIntField(ref _textureHeight, 1, "Texture Height");

                // Add the toggle for list sorting
                _sortStopsByTime =
                    EditorGUILayout.Toggle(new GUIContent("Sort gradient stops"), _sortStopsByTime);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// This method previews the texture on the GUI
        /// </summary>
        private void DrawPreviewTextureGUI()
        {
            if (_previewTexture == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            CustomEditorGUI.Title("Gradient Texture Preview", EditorStyles.boldLabel);

            using (new eUtility.IndentedLayout())
            {
                var target = HotspotGUIUtility.IndentedRect(GUILayoutUtility.GetRect(position.width, 256));
                EditorGUI.DrawTextureTransparent(target, _previewTexture);
                CustomEditorGUI.DrawBorders(target, 5, Color.black);
            }
        }

        /// <summary>
        /// This methods draws the save settings
        /// </summary>
        private void DrawSaveGUI()
        {
            CustomEditorGUI.Title("Save Texture file");

            _fileFormat = (TextureFactory.FileSaveFormat)EditorGUILayout.EnumPopup("File Format", _fileFormat);
            _overwriteFile = EditorGUILayout.Toggle("Overwrite File", _overwriteFile);

            if (GUILayout.Button("Save gradient texture."))
            {
                _path = EditorUtility.SaveFilePanel("Save gradient texture", _path,
                    "Texture", _fileFormat.ToString().ToLowerInvariant());

                if (_path.IsNullOrEmpty())
                    return;

                var saveTexture = CreateTexture(_textureWidth, _textureHeight);
                if (!TextureFactory.SaveGradientTexture(saveTexture, _path, _fileFormat, _overwriteFile))
                    PLog.Error<HotspotLogger>(
                        "[Gradient Texture Creator, DrawTextureSaveGUI] Failed to save texture to: " + _path);
            }
        }

        /// <summary>
        /// Creates the preview texture
        /// </summary>
        private Texture2D CreateTexture(int width, int height)
        {
            var stopDictionary = CreateStopDictionary();
            return TextureFactory.Create2DGradientTexture(width, height, stopDictionary);
        }

        /// <summary>
        /// Creates the stop sorted dictionary for the gradient texture creation.
        /// Adds some checks to make sure the list is correct,
        /// </summary>
        /// <returns>A sorted dictionary with all the stops.</returns>
        private SortedDictionary<float, Color> CreateStopDictionary()
        {
            var stopDictionary = new SortedDictionary<float, Color>();
            foreach (GradientColorKey stop in _gradientStops)
            {
                if (!stopDictionary.ContainsKey(stop.time))
                {
                    stopDictionary.Add(stop.time, stop.color);
                    continue;
                }

                float stopTemp = stop.time;
                while (stopTemp <= 1.0f)
                {
                    stopTemp += 0.01f;
                    if (!stopDictionary.ContainsKey(stopTemp))
                    {
                        stopDictionary.Add(stopTemp, stop.color);
                        break;
                    }
                }
            }

            return stopDictionary;
        }

        private float CalculateElementHeight()
        {
            return EditorGUIUtility.singleLineHeight * 6;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!index.IsBetweenIncl(0, _gradientStops.Count - 1))
            {
                return;
            }

            var element = _gradientStops[index];
            GUILayout.BeginArea(rect);
            EditorGUILayout.LabelField($"Gradient stop {index}", EditorStyles.boldLabel);
            using (new eUtility.IndentedLayout())
            {
                EditorGUILayout.LabelField("Colour:", EditorStyles.boldLabel);
                element.color = EditorGUILayout.ColorField(element.color);
            }

            using (new eUtility.IndentedLayout())
            {
                EditorGUILayout.LabelField("Stop amount:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                element.time = EditorGUILayout.FloatField(element.time);
                element.time = Mathf.Clamp01(element.time);
                EditorGUILayout.EndHorizontal();
            }

            _gradientStops[index] = element;
            GUILayout.EndArea();
        }
    }
}