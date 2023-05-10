using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hotspot.Editor
{
    public class MaterialAnalysisInfo : AnalysisInfo
    {
        private int _amountOfOccurrences;
        public int AmountOfOccurrences => _amountOfOccurrences;

        private Material _material;
        private bool _isUnfolded;
        private IEnumerable<KeyValuePair<Mesh, GameObject>> _materialUses;
        private IEnumerable<KeyValuePair<string, int>> _keyWordCombinations;

        public MaterialAnalysisInfo(Material material, int amountOfOccurrences, IEnumerable<Renderer> renderers,
            IEnumerable<Material> allMaterials)
        {
            _material = material;
            _amountOfOccurrences = amountOfOccurrences;
            _materialUses = GetMaterialUses(renderers);
            GetKeywordSpread(allMaterials);
        }

        private void GetKeywordSpread(IEnumerable<Material> allMaterials)
        {
            // Get all materials that match this info's material
            var materials = allMaterials.Where(x => x == _material);

            // Get all combinations of keywords
            var keyWordCombinations = materials.Select(x => x.enabledKeywords);

            // Group them according to their distinct set and count them
            var groups = keyWordCombinations.GroupBy(x => new HashSet<LocalKeyword>(x))
                .Select(g => new KeyValuePair<IEnumerable<LocalKeyword>, int>(g.Key, g.Count()));

            // Create the return value
            Dictionary<string, int> returnValue = new ();
            
            // Process all the groups
            foreach (var group in groups)
            {
                string keywordCombo = group.Key.Aggregate(string.Empty, (current, keyWords) => current + (keyWords.ToString() + " "));
                if (returnValue.ContainsKey(keywordCombo))
                    returnValue[keywordCombo] += group.Value;
                else
                    returnValue.Add(keywordCombo, group.Value);
            }

            // Replace the entry with an empty string as key
            foreach (var pair in returnValue)
            {
                if (pair.Key == string.Empty)
                {
                    int pairValue = returnValue[pair.Key];
                    string newKey = "NO_KEYWORDS";
                    returnValue.Add(newKey, pairValue);
                    returnValue.Remove(pair.Key);
                    break;
                }
            }
            
            // Set the value
            _keyWordCombinations = returnValue;
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

            GUILayout.Space(10f);
            GUILayout.Label("Material Keyword Combinations", EditorStyles.boldLabel);
            GUILayout.Label($"Amount of combinations: {_keyWordCombinations.Count()}", EditorStyles.boldLabel);
            foreach (var pair in _keyWordCombinations)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pair.Key.ToString());
                EditorGUILayout.LabelField(pair.Value.ToString());
                GUILayout.EndHorizontal();
            }

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