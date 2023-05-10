using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Editor;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Hotspot;
using Rhinox.Hotspot.Editor;


using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using static PlasticGui.PlasticTableColumn;

namespace Hotspot.Editor
{
    public class OcclusionPortalWindow : CustomEditorWindow
    {

        private Dictionary<GameObject, OcclusionPortal> _occlusionPortalDictionary = new Dictionary<GameObject, OcclusionPortal>();

        private Vector2 _scrollPos;
        //OcclusionsPortalsList _portalalsList = null;

        [MenuItem("Tools/HotSpot/Occlusion Portal Editor", false, 50)]

        public static void ShowWindow()
        {
            var win = GetWindow(typeof(OcclusionPortalWindow));
            win.titleContent = new GUIContent("Occlusion Portals");
        }

        private void GeneratePortals()
        {
            _occlusionPortalDictionary.Clear();

            var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                        return;

                    if (mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
                    {
                        var portal = renderer.gameObject.GetOrAddComponent<OcclusionPortal>();
                        portal.UpdateBounds(renderer.localBounds);
                        _occlusionPortalDictionary.Add(renderer.gameObject, portal);
                        break;
                    }
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
            //base.OnGUI();
            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate New Portals", GUILayout.ExpandWidth(true)))
                GeneratePortals();
            if (GUILayout.Button("Get All Portals Scene ", GUILayout.ExpandWidth(true)))
                GetAllPortalsInScene();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Button("Load Portals From File", GUILayout.ExpandWidth(true));
            GUILayout.Button("Save Portals From File", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5f);

            GUIStyle gUIStyle = new GUIStyle()
            {
            };

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true), GUILayout.MaxHeight(300f));
            foreach (var item in _occlusionPortalDictionary)
            {
                EditorGUILayout.BeginHorizontal();
                //EditorGUILayout.BeginVertical();
                //EditorGUILayout.PropertyField(new SerializedObject(item.Value).FindProperty("m_Center"));
                //EditorGUILayout.PropertyField(new SerializedObject(item.Value).FindProperty("m_Size"));
                //EditorGUILayout.EndVertical();
                EditorGUILayout.LabelField($"{item.Key.name}");

                if (GUILayout.Button("GOTO"))
                {
                    Selection.activeObject = item.Value;
                    EditorGUIUtility.PingObject(item.Key);
                    SceneView.lastActiveSceneView.Frame(new Bounds(item.Value.GetBounds().center, Vector3.one), false);
                }
                //EditorGUILayout.LabelField($"{item.Value.name}");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Button("<-- Previous", GUILayout.ExpandWidth(true));
            GUILayout.Button("Discard Portal", GUILayout.ExpandWidth(true));
            GUILayout.Button("Next -->", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5f);



            EditorGUILayout.EndVertical();
        }
    }
}