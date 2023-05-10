using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public class MaterialAnalysisInfo : AnalysisInfo
    {
        private Material _material;
        private int _amountOfOccurrences;
        private bool _isUnfolded;
        private IEnumerable<KeyValuePair<Mesh, GameObject>> _materialUses;

        public MaterialAnalysisInfo(Material material, int amountOfOccurrences, IEnumerable<Renderer> renderers)
        {
            _material = material;
            _amountOfOccurrences = amountOfOccurrences;
            _materialUses = GetMaterialUses(renderers);
        }

        private IEnumerable<KeyValuePair<Mesh, GameObject>> GetMaterialUses(IEnumerable<Renderer> renderers)
        {
            var gameObjectsWithTargetMaterial =
                renderers.Where(r => r.sharedMaterials.Any(m => m == _material)).Select(r => r.gameObject)
                    .ToArray();

            var returnVal = from gameObject in gameObjectsWithTargetMaterial
                let mesh = gameObject.GetComponent<MeshFilter>().sharedMesh
                select new KeyValuePair<Mesh, GameObject>(mesh, gameObject);

            return returnVal;
        }

        protected override void DrawObjectField()
        {
            using (new eUtility.DisabledGroup())
            {
                EditorGUILayout.LabelField($"{_material.name}: {_amountOfOccurrences}");
                EditorGUILayout.ObjectField(_material, typeof(Material), false);
            }
        }

        protected override void DrawUnfoldedGUI()
        {
            GUILayout.Label("Material Info", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Shader: ");
            EditorGUILayout.LabelField(_material.shader.name);
            GUILayout.EndHorizontal();

            foreach (string keywordName in _material.shaderKeywords)
            {
                GUILayout.Label(keywordName, EditorStyles.miniLabel);
            }

            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Applied Mesh", EditorStyles.boldLabel);
            GUILayout.Label("GameObject Full name", EditorStyles.boldLabel);
            GUILayout.Label("Ping in Hierarchy", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            GUILayout.EndHorizontal();

            foreach (var materialUsePair in _materialUses)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{materialUsePair.Key.name}");
                GUILayout.Label($"{materialUsePair.Value.GetFullName()}");
                if (GUILayout.Button("Ping", GUILayout.Width(EditorGUIUtility.labelWidth)))
                {
                    EditorGUIUtility.PingObject(materialUsePair.Value);
                }
            
                GUILayout.EndHorizontal();
            }
        }
    }
}