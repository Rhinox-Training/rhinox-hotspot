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
    private const float _scrollViewMaxHeight = 200f;
    private const float _normalButtonHeight = 30f;
    private const float _RemoveAllButtonHeight = 20f;
    private const int _navMeshAreaCount = 32;

    private GameObject _rootObj = null;

    [NavMeshArea(true)]
    public int _navMeshAreasMask = -1;

    private SerializedObject _serObj = null;
    private SerializedProperty _serProp = null;

    private float _height = 2.5f;
    private float _margin = .5f;

    //Dictionary<GameObject, OcclusionArea> _occlusionAreas = new Dictionary<GameObject, OcclusionArea>();
    HashSet<OcclusionArea> _occlusionAreas = new HashSet<OcclusionArea>();
    private Vector2 _scrollPos;
    private int _selectedAreaIndex;

    [MenuItem(HotspotWindowHelper.TOOLS_PREFIX +"Occlusion Area Editor", false, 50)]
    public static void ShowWindow()
    {
        var win = GetWindow(typeof(OcclusionAreaWindow));
        win.titleContent = new GUIContent("Occlusion Wizard");
    }

    protected override void Initialize()
    {
        base.Initialize();

        _serObj = new SerializedObject(this);
        _serProp = _serObj.FindProperty(nameof(_navMeshAreasMask));
    }

    private void GenerateOcclusionAreas()
    {
        GetAllOcclusionAreas();

        if (_rootObj == null)
            _rootObj = new GameObject(_rootObjName);

        var boundsInformations = NavMeshHelper.GetNavMeshBounds(_height, _navMeshAreasMask, _margin);

        int meshIdx = 0;
        foreach (var boundsList in boundsInformations)
        {
            int boundIdx = 0;
            foreach (var bound in boundsList.ConvexDecomposedBounds)
            {
                string autoGenName = $"_Auto_OcclusionArea_{boundsList.Area}_Mesh{meshIdx}_Bound{boundIdx}";
                CreateOrAddNewOcclusionArea(bound, autoGenName);
                ++boundIdx; ;
            }
            ++meshIdx;
        }
    }

    private void CreateOrAddNewOcclusionArea(Bounds bounds, string autoGenName)
    {
        GameObject obj = GameObject.Find(autoGenName);
        obj = obj != null ? obj : new GameObject(autoGenName);

        var occlusionAreaCmpt = obj.GetOrAddComponent<OcclusionArea>();
        obj.transform.parent = _rootObj.transform;

        bounds.center = bounds.center.With(null, bounds.center.y + _height / 2f, null);
        bounds.size = bounds.size.With(null, _height, null);
        occlusionAreaCmpt.center = bounds.center;
        occlusionAreaCmpt.size = bounds.size;
        //occlusionAreaCmpt.center = new Vector3(recto.x + (recto.width) / 2, nave.vertices[0].y + (_height / 2), recto.y + (recto.height) / 2);
        //occlusionAreaCmpt.size = new Vector3(recto.width + (_margin * 2), _height, recto.height + (_margin * 2));

        _occlusionAreas.Add(occlusionAreaCmpt);
    }

    private void GetAllOcclusionAreas()
    {
        _rootObj = _rootObj != null ? _rootObj : GameObject.Find(_rootObjName);

        var occlusionAreas = UnityEngine.Object.FindObjectsOfType<OcclusionArea>();

        foreach (var area in occlusionAreas)
        {
            _occlusionAreas.Add(area);
        }
    }

    protected override void OnGUI()
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(_serProp);
        if (_serObj.hasModifiedProperties)
            _serObj.ApplyModifiedProperties();
        EditorGUILayout.Space(5f);
        EditorGUILayout.BeginVertical();

        _height = EditorGUILayout.FloatField("Area height: ", _height, GUILayout.ExpandWidth(true));
        _margin = EditorGUILayout.FloatField("Area margin", _margin, GUILayout.ExpandWidth(true));

        EditorGUILayout.Space(5f);

        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate New Areas", GUILayout.Height(_normalButtonHeight), GUILayout.ExpandWidth(true)))
                GenerateOcclusionAreas();
            if (GUILayout.Button("Get All Areas Scene", GUILayout.Height(_normalButtonHeight), GUILayout.ExpandWidth(true)))
                GetAllOcclusionAreas();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5f);
        if (GUILayout.Button("REMOVE ALL OCCLUSION AREAS IN SCENE", GUILayout.Height(_RemoveAllButtonHeight), GUILayout.ExpandWidth(true)))
            RemoveAllAreas();
        EditorGUILayout.Space(5f);

        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true), GUILayout.MaxHeight(_scrollViewMaxHeight));
            ShowExistingAreas();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(5f);

        {
            EditorGUILayout.BeginHorizontal();
            ProcessFooterButtons();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void RemoveAllAreas()
    {
        _occlusionAreas.Clear();
        var areas = UnityEngine.Object.FindObjectsOfType<OcclusionArea>();

        foreach (var area in areas)
            DestroyImmediate(area);
    }

    //makes a list that shows all the objects who have an occlusion portal on them.
    //Also give them a button, so you can ping them in the hierachy and focus them in the scene view.
    private void ShowExistingAreas()
    {
        float _width = GUI.skin.label.CalcSize(new GUIContent("#9999:")).x;

        int index = 0;
        foreach (var area in _occlusionAreas)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{index}", GUILayout.Width(_width));

            if (index != _selectedAreaIndex)
            {
                if (GUILayout.Button($"{area.gameObject.name}", GUILayout.ExpandWidth(true)))
                {
                    SelectItemInScene(area);
                    _selectedAreaIndex = index;
                }
            }
            else
            {
                using (new eUtility.GuiBackgroundColor(Color.gray))
                {
                    if (GUILayout.Button($"{area.gameObject.name}", GUILayout.ExpandWidth(true)))
                        SelectItemInScene(area);
                }
            }

            EditorGUILayout.EndHorizontal();
            ++index;
        }
    }


    //logice to show the bottom buttons (prev, discard, next)
    //the prev and next are just decrement and increment with bound checks.
    //the discard removes the occlusion portal component and removes it from the dictionary (with bounds check)
    private void ProcessFooterButtons()
    {
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
                    var area = _occlusionAreas.ElementAt(_selectedAreaIndex);
                    _occlusionAreas.Remove(area);
                    DestroyImmediate(area);

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
    }

    //simple function to set the gameobject as selected
    //ping the object
    //and focus the scene editor camera to the object.
    private void SelectItemInScene(OcclusionArea area)
    {
        Selection.activeObject = area.gameObject;
        EditorGUIUtility.PingObject(area);
        SceneView.lastActiveSceneView.Frame(new Bounds(area.center + area.gameObject.transform.position, Vector3.one), false);
    }
}
