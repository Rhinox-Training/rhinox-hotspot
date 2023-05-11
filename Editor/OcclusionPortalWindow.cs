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

        public List<Material> _allowedMaterials = new List<Material>();
        public List<string> _allowedMeshNameKeywords = new List<string>();

        private Dictionary<GameObject, OcclusionPortal> _occlusionPortalDictionary = new Dictionary<GameObject, OcclusionPortal>();
        private Vector2 _scrollPos;
        private int _selectedPortalIndex = -1;
        private Vector3 _boundsMargin = new Vector3(0.05f, 0.05f, 0.05f);

        private SerializedObject _serObj;
        private SerializedProperty _serMatListProp;
        private SerializedProperty _serKeyWordListProp;

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
            _serMatListProp = _serObj.FindProperty(nameof(_allowedMaterials));
            _serKeyWordListProp = _serObj.FindProperty(nameof(_allowedMeshNameKeywords));


            _occlusionPortalDictionary ??= new Dictionary<GameObject, OcclusionPortal>();

            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => _occlusionPortalDictionary?.Clear();

        private void GeneratePortals()
        {
            _occlusionPortalDictionary.Clear();

            var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>();

                //check if the renderer his meshfilter meets the requirements
                if (renderer.sharedMaterials.ContainsAny(_allowedMaterials) &&
                    filter.sharedMesh.name.ContainsOneOf(_allowedMeshNameKeywords.ToArray()))
                //    if (renderer.sharedMaterials.ContainsAny(_allowedMaterials))
                {
                    //    if (filter.sharedMesh.name.ContainsOneOf(_allowedMeshNameKeywords.ToArray()))
                    //    {
                    var portal = renderer.gameObject.GetOrAddComponent<OcclusionPortal>();
                    var localMargin = renderer.transform.InverseTransformVector(_boundsMargin);
                    portal.UpdateBounds(renderer.localBounds.AddMarginToExtends(localMargin.Abs()));

                    _occlusionPortalDictionary.Add(renderer.gameObject, portal);
                    //}
                }


                //foreach (var mat in renderer.sharedMaterials)
                //{
                //    if (mat == null)
                //        return;

                //    if (mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
                //    {
                //        var portal = renderer.gameObject.GetOrAddComponent<OcclusionPortal>();
                //        var localMargin = renderer.transform.InverseTransformVector(_boundsMargin);
                //        portal.UpdateBounds(renderer.localBounds.AddMarginToExtends(localMargin.Abs()));

                //        _occlusionPortalDictionary.Add(renderer.gameObject, portal);
                //        break;
                //    }
                //}
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
                EditorGUILayout.PropertyField(_serMatListProp);
                EditorGUILayout.PropertyField(_serKeyWordListProp);
                EditorGUILayout.Space(7f);
                if (_serObj.hasModifiedProperties)
                    _serObj.ApplyModifiedProperties();
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

        private void SelectItemInScene(KeyValuePair<GameObject, OcclusionPortal> item)
        {
            Selection.activeObject = item.Value;
            EditorGUIUtility.PingObject(item.Key);
            //var rendererBound = item.Value.GetComponent<Renderer>().bounds;
            //rendererBound.center += item.Key.transform.position;
            //SceneView.lastActiveSceneView.Frame(rendererBound, false);
            SceneView.lastActiveSceneView.Frame(item.Value.GetComponent<Renderer>().bounds, false);
        }
    }
}