using Hotspot.Editor;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class OcclusionAreaWindow : CustomEditorWindow
{
    private const string _rootObjName = "__GeneratedOcllusionAreas__";
    private const int _navMeshAreaCount = 32;
    private const float _scrollViewMaxHeight = 150f;
    private GameObject _rootObj = null;

    [NavMeshArea(true)]
    public int _navMeshAreas = -1;

    private SerializedObject _serObj = null;
    private SerializedProperty _serProp = null;

    private float _height = 2.5f;
    private float _margin = .5f;

    //private object _layerMask;

    Dictionary<GameObject, OcclusionArea> _occlusionAreas = new Dictionary<GameObject, OcclusionArea>();
    private Vector2 _scrollPos;
    private int _selectedAreaIndex;

    //

    [MenuItem("Tools/HotSpot/Occlusion Area Editor", false, 50)]

    public static void ShowWindow()
    {
        var win = GetWindow(typeof(OcclusionAreaWindow));
        win.titleContent = new GUIContent("Occlusion Wizard");
    }

    protected override void Initialize()
    {
        base.Initialize();

        _serObj = new SerializedObject(this);
        _serProp = _serObj.FindProperty("_navMeshAreas");
    }

    private void GenerateOcclusionAreas()
    {
        GetAllOcclusionAreas();

        if (_rootObj == null)
            _rootObj = new GameObject(_rootObjName);

        for (int idx = 0; idx < _navMeshAreaCount; idx++)
        {
            if (((_navMeshAreas >> idx) & 1) == 0)
                continue;

            var nave = NavMeshHelper.CalculateTriangulation(1 << idx);
            if (nave.vertices.Length == 0)
                continue;

            Rect recto = new Rect(nave.vertices[0].x, nave.vertices[0].z, 0f, 0f);

            for (int verteIdx = 1; verteIdx < nave.vertices.Length; ++verteIdx)
            {
                recto = recto.Encapsulate(new Vector2(nave.vertices[verteIdx].x, nave.vertices[verteIdx].z));
            }

            GameObject obj = GameObject.Find($"OcclusionArea{idx}");
            obj = obj != null ? obj : new GameObject($"OcclusionArea{idx}");
            //if (_occlusionAreas.TryGetValue())
            //{

            //}

            var occlusionAreaCmpt = obj.GetOrAddComponent<OcclusionArea>();
            obj.transform.parent = _rootObj.transform;

            occlusionAreaCmpt.center = new Vector3(recto.x + (recto.width) / 2, nave.vertices[0].y + (_height / 2), recto.y + (recto.height) / 2);
            occlusionAreaCmpt.size = new Vector3(recto.width + (_margin * 2), _height, recto.height + (_margin * 2));

            _occlusionAreas.TryAdd(obj, occlusionAreaCmpt);
        }
    }

    private void GetAllOcclusionAreas()
    {
        _rootObj = _rootObj != null ? _rootObj : GameObject.Find(_rootObjName);

        if (_rootObj != null)
        {
            int cnt = _rootObj.transform.childCount;

            for (int idx = 0; idx < cnt; ++idx)
            {
                var child = _rootObj.transform.GetChild(idx);
                if (child.TryGetComponent<OcclusionArea>(out var cmpnt))
                    _occlusionAreas.Add(child.gameObject, cmpnt);
            }
        }

        //var OcclusionAreas = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
    }

    private void SelectItemInScene(KeyValuePair<GameObject, OcclusionArea> item)
    {
        Selection.activeObject = item.Value;
        EditorGUIUtility.PingObject(item.Key);
        SceneView.lastActiveSceneView.Frame(new Bounds(item.Value.center, item.Value.size), false);
    }

    protected override void OnGUI()
    {
        float _width = GUI.skin.label.CalcSize(new GUIContent("#9999:")).x;

        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(_serProp);
        if (_serObj.hasModifiedProperties)
            _serObj.ApplyModifiedProperties();
        EditorGUILayout.Space(5f);
        EditorGUILayout.BeginVertical();

        _height = EditorGUILayout.FloatField("Area height: ", _height, GUILayout.ExpandWidth(true));
        _margin = EditorGUILayout.FloatField("Area margin", _margin, GUILayout.ExpandWidth(true));

        EditorGUILayout.Space(5f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate New Areas", GUILayout.ExpandWidth(true)))
            GenerateOcclusionAreas();
        if (GUILayout.Button("Get All Areas Scene ", GUILayout.ExpandWidth(true)))
            GetAllOcclusionAreas();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5f);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true), GUILayout.MaxHeight(_scrollViewMaxHeight));

        int index = 0;
        foreach (var item in _occlusionAreas)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{index}", GUILayout.Width(_width));

            if (index != _selectedAreaIndex)
            {
                if (GUILayout.Button($"{item.Key.name}", GUILayout.ExpandWidth(true)))
                {
                    SelectItemInScene(item);
                    _selectedAreaIndex = index;
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

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(5f);
        EditorGUILayout.BeginHorizontal();

        using (new eUtility.DisabledGroup(_occlusionAreas.Count == 0))
        {
            if (GUILayout.Button("<-- Previous", GUILayout.ExpandWidth(true)))
            {
                --_selectedAreaIndex;
                if (_selectedAreaIndex <= 0)
                    _selectedAreaIndex = 0;

                SelectItemInScene(_occlusionAreas.ElementAt(_selectedAreaIndex));
            }

            if (GUILayout.Button("Discard Occlusion Area", GUILayout.ExpandWidth(true)))
            {
                if (_selectedAreaIndex >= 0 && _selectedAreaIndex < _occlusionAreas.Count)
                {
                    var pair = _occlusionAreas.ElementAt(_selectedAreaIndex);
                    DestroyImmediate(pair.Value);
                    _occlusionAreas.Remove(pair.Key);

                    if (_selectedAreaIndex >= _occlusionAreas.Count)
                        _selectedAreaIndex = _occlusionAreas.Count - 1;

                    SelectItemInScene(_occlusionAreas.ElementAt(_selectedAreaIndex));
                }
            }

            if (GUILayout.Button("Next -->", GUILayout.ExpandWidth(true)))
            {
                ++_selectedAreaIndex;
                if (_selectedAreaIndex >= _occlusionAreas.Count)
                    _selectedAreaIndex = _occlusionAreas.Count - 1;

                SelectItemInScene(_occlusionAreas.ElementAt(_selectedAreaIndex));
            }
        }

        EditorGUILayout.EndHorizontal();

        //_navMeshAreas = EditorGUILayout.MaskField(_navMeshAreas, _layerMaskOptions.ToArray());

        EditorGUILayout.EndVertical();
    }
}
