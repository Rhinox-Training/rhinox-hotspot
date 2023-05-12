using Rhinox.Lightspeed;
using Rhinox.GUIUtils.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;

namespace Hotspot.Editor
{
    public class OcclusionPortalWindow : CustomEditorWindow
    {
        private const float _scrollViewMaxHeight = 300f;
        private const float _normalButtonHeight = 30f;
        private const float _RemoveAllButtonHeight = 20f;

        [SerializeReference]
        public List<IRendererFilter> _rendererFilters = new List<IRendererFilter>();

        private Dictionary<GameObject, OcclusionPortal> _occlusionPortalDictionary = new Dictionary<GameObject, OcclusionPortal>();
        private Vector2 _scrollPos;
        private int _selectedPortalIndex = -1;
        private Vector3 _boundsMargin = new Vector3(0.05f, 0.05f, 0.05f);

        private SerializedObject _serObj;
        private SerializedProperty _serFilterListProp;
        private ListDrawable _rendererFiltersDrawer;

        [MenuItem("Tools/HotSpot/Occlusion Portal Editor", false, 50)]

        public static void ShowWindow()
        {
            var win = GetWindow(typeof(OcclusionPortalWindow));
            win.titleContent = new GUIContent("Occlusion Portals");
        }

        protected override void Initialize()
        {
            base.Initialize();

            _serObj = new SerializedObject(this);
            _serFilterListProp = _serObj.FindProperty(nameof(_rendererFilters));
            _rendererFiltersDrawer = new ListDrawable(_serFilterListProp);

            _occlusionPortalDictionary ??= new Dictionary<GameObject, OcclusionPortal>();

            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => _occlusionPortalDictionary?.Clear();

        //Gets all meshrenderers in the scene.
        //loops over them and check if they meet all the filter conditions
        //if so, create/get the occlusion portal, set the dimension and add it to dictionary
        private void GeneratePortals()
        {
            _occlusionPortalDictionary.Clear();

            var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (var renderer in renderers)
            {
                if (_rendererFilters.TrueForAll(x => x.IsValid(renderer)))
                {
                    var portal = renderer.gameObject.GetOrAddComponent<OcclusionPortal>();
                    var localMargin = renderer.transform.InverseTransformVector(_boundsMargin);
                    portal.UpdateBounds(renderer.localBounds.AddMarginToExtends(localMargin.Abs()));

                    _occlusionPortalDictionary.Add(renderer.gameObject, portal);
                }
            }
        }

        private void GetAllPortalsInScene()
        {
            _occlusionPortalDictionary.Clear();

            var portals = UnityEngine.Object.FindObjectsOfType<OcclusionPortal>();

            foreach (var portal in portals)
            {
                _occlusionPortalDictionary.Add(portal.gameObject, portal);
            }
        }

        protected override void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(5f);

            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate New Portals", GUILayout.Height(_normalButtonHeight), GUILayout.ExpandWidth(true)))
                    GeneratePortals();
                if (GUILayout.Button("Get All Portals Scene", GUILayout.Height(_normalButtonHeight), GUILayout.ExpandWidth(true)))
                    GetAllPortalsInScene();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5f);

            if (GUILayout.Button("REMOVE ALL PORTALS IN SCENE", GUILayout.Height(_RemoveAllButtonHeight), GUILayout.ExpandWidth(true)))
                RemoveAllPortals();

            EditorGUILayout.Space(5f);

            _boundsMargin = EditorGUILayout.Vector3Field("Portal bounds margin: ", _boundsMargin, GUILayout.ExpandWidth(true));

            {
                EditorGUILayout.Space(7f);

                if (_rendererFiltersDrawer!=null)
                {
                    _rendererFiltersDrawer.Draw(GUIContent.none);
                }
                EditorGUILayout.Space(7f);
                //if (_serObj.hasModifiedProperties)
                //    _serObj.ApplyModifiedProperties();
            }


            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true), GUILayout.MaxHeight(_scrollViewMaxHeight));

            ShowExistingOcclusionPortals();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            HandleFooterButtons();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5f);

            EditorGUILayout.EndVertical();
        }

        private void RemoveAllPortals()
        {
            _occlusionPortalDictionary.Clear();
            var portals = UnityEngine.Object.FindObjectsOfType<OcclusionPortal>();

            foreach (var portal in portals)
                DestroyImmediate(portal);
        }

        //makes a list that shows all the objects who have an occlusion portal on them.
        //Also give them a button, so you can ping them in the hierachy and focus them in the scene view.
        private void ShowExistingOcclusionPortals()
        {
            float _width = GUI.skin.label.CalcSize(new GUIContent("#9999:")).x;

            int index = 0;
            foreach (var item in _occlusionPortalDictionary)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"#{index}", GUILayout.Width(_width));

                if (index != _selectedPortalIndex)
                {
                    if (GUILayout.Button($"{item.Key.name}", GUILayout.ExpandWidth(true)))
                    {
                        SelectItemInScene(item);
                        _selectedPortalIndex = index;
                    }
                }
                else
                {
                    using (new eUtility.GuiBackgroundColor(Color.gray))
                    {
                        if (GUILayout.Button($"{item.Key.name}", GUILayout.ExpandWidth(true)))
                            SelectItemInScene(item);
                    }
                }

                EditorGUILayout.EndHorizontal();
                ++index;
            }
        }

        //logice to show the bottom buttons (prev, discard, next)
        //the prev and next are just decrement and increment with bound checks.
        //the discard removes the occlusion portal component and removes it from the dictionary (with bounds check)
        private void HandleFooterButtons()
        {
            using (new eUtility.DisabledGroup(_occlusionPortalDictionary.Count == 0))
            {
                if (GUILayout.Button("<-- Previous", GUILayout.ExpandWidth(true)))
                {
                    --_selectedPortalIndex;
                    if (_selectedPortalIndex <= 0)
                        _selectedPortalIndex = 0;

                    SelectItemInScene(_occlusionPortalDictionary.ElementAt(_selectedPortalIndex));
                }

                if (GUILayout.Button("Discard Portal", GUILayout.ExpandWidth(true)))
                {
                    if (_selectedPortalIndex >= 0 && _selectedPortalIndex < _occlusionPortalDictionary.Count)
                    {
                        var pair = _occlusionPortalDictionary.ElementAt(_selectedPortalIndex);
                        DestroyImmediate(pair.Value);
                        _occlusionPortalDictionary.Remove(pair.Key);

                        if (_selectedPortalIndex >= _occlusionPortalDictionary.Count)
                            _selectedPortalIndex = _occlusionPortalDictionary.Count - 1;

                        SelectItemInScene(_occlusionPortalDictionary.ElementAt(_selectedPortalIndex));
                    }
                }

                if (GUILayout.Button("Next -->", GUILayout.ExpandWidth(true)))
                {
                    ++_selectedPortalIndex;
                    if (_selectedPortalIndex >= _occlusionPortalDictionary.Count)
                        _selectedPortalIndex = _occlusionPortalDictionary.Count - 1;

                    SelectItemInScene(_occlusionPortalDictionary.ElementAt(_selectedPortalIndex));
                }
            }
        }
        
        //simple function to set the gameobject as selected
        //ping the object
        //and focus the scene editor camera to the object.
        private void SelectItemInScene(KeyValuePair<GameObject, OcclusionPortal> item)
        {
            Selection.activeObject = item.Value;
            EditorGUIUtility.PingObject(item.Key);
            SceneView.lastActiveSceneView.Frame(item.Value.GetComponent<Renderer>().bounds, false);
        }
    }
}