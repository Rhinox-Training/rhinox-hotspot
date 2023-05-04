using Rhinox.GUIUtils.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Hotspot.Editor
{
    public class DenseVertexSpotWindow : CustomEditorWindow
    {
        private Vector2 _scrollPos = Vector2.zero;
        private Octree _octTree = null;
        private List<KeyValuePair<int, Vector3>> _hotList = new List<KeyValuePair<int, Vector3>>();

        public static void ShowWindow()
        {
            var win = GetWindow(typeof(DenseVertexSpotWindow));
            win.titleContent = new GUIContent("HotSpots");
        }

        public void UpdateTree(Octree tree)
        {
            _octTree = tree;

            UpdateHotSpotList();
        }

        private void RecursiveHotSpotFinder(Octree tree)
        {
            if (tree.children == null)
            {
                if (tree.VertexCount > 0)
                {
                    _hotList.Add(new KeyValuePair<int, Vector3>(tree.VertexCount, tree._bounds.center));
                }
                return;
            }

            foreach (var child in tree.children)
            {
                RecursiveHotSpotFinder(child);
            }
        }

        private void UpdateHotSpotList()
        {
            _hotList.Clear();

            RecursiveHotSpotFinder(_octTree);

            _hotList.Sort((pair1, pair2) => pair2.Key.CompareTo(pair1.Key));
        }

        protected override void OnGUI()
        {

            EditorGUILayout.Space(10f);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            float width = GUI.skin.label.CalcSize(new GUIContent("#9999:")).x;

            float widthCount = GUI.skin.label.CalcSize(new GUIContent("Vertex Count: 999.999")).x;

            GUIStyle styleLabelPos = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight
            };

            GUIStyle styleLabelCount = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft
            };

            int index = 0;
            foreach (var item in _hotList)
            {
                EditorGUILayout.BeginHorizontal();
                ++index;

                string est = string.Format(new CultureInfo("nl-BE"), $"Vertex Count: {item.Key:N0}");
                //GUILayout.wid
                GUILayout.Label($"#{index}:", styleLabelPos, GUILayout.MaxWidth(width));
                GUILayout.Label(est, styleLabelCount, GUILayout.MaxWidth(widthCount));
                if (GUILayout.Button("GOTO"/*, GUILayout.Width(75f)*/))
                {
                    SceneView.lastActiveSceneView.Frame(new Bounds(item.Value, Vector3.one), false);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}