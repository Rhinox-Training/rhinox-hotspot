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
        private const int _minListLength = 10;
        private const int _maxListLength = 250;
        private int _hotSpotListLength = 100;

        private int _hotSpotTreshold = 0;
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

        public void UpdateTreshold(int treshold)
        {
            _hotSpotTreshold = treshold;
        }

        private void RecursiveHotSpotFinder(Octree tree)
        {
            if (tree.children == null)
            {
                if (tree.VertexCount > _hotSpotTreshold)
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
            EditorGUILayout.Space(5f);
            _hotSpotListLength = EditorGUILayout.IntSlider(_hotSpotListLength, _minListLength, _maxListLength);
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


            for (int index = 0; index < _hotList.Count; ++index)
            {
                if (index > _hotSpotListLength)
                    break;

                EditorGUILayout.BeginHorizontal();

                //GUILayout.wid
                GUILayout.Label($"#{index}:", styleLabelPos, GUILayout.MaxWidth(width));
                GUILayout.Label($"Vertex Count: {_hotList[index].Key}", styleLabelCount, GUILayout.MaxWidth(widthCount));
                if (GUILayout.Button("GOTO"))
                {
                    SceneView.lastActiveSceneView.Frame(new Bounds(_hotList[index].Value, Vector3.one), false);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}