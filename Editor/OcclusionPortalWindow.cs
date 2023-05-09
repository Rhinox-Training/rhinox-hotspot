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
                        portal.UpdateBounds(renderer.bounds);
                        _occlusionPortalDictionary.Add(renderer.gameObject, portal);
                        break;
                    }
                }
            }
        }

        protected override void OnGUI()
        {
            //base.OnGUI();
            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Generate Portals", GUILayout.ExpandWidth(true)))
                GeneratePortals();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Button("Load Portals", GUILayout.ExpandWidth(true));
            GUILayout.Button("Save Portals", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5f);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(300f));
            foreach (var item in _occlusionPortalDictionary)
            {

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