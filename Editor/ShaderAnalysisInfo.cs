using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Hotspot.Editor
{
    public class ShaderAnalysisInfo : AnalysisInfo
    {
        private Shader _shader;
        private int _amountOfOccurrences;
        IEnumerable<KeyValuePair<Material, int>> _shaderUses;

        public ShaderAnalysisInfo(Shader shader, int amountOfOccurrences, IEnumerable<Renderer> renderers)
        {
            _shader = shader;
            _amountOfOccurrences = amountOfOccurrences;
            _shaderUses = GetShaderUses(renderers);
        }

        private IEnumerable<KeyValuePair<Material, int>> GetShaderUses(IEnumerable<Renderer> renderers)
        {
            var materials = renderers.SelectMany(x => x.sharedMaterials).ToArray();
            var materialsWithShaderInstance = materials.Where(m => m.shader.name == _shader.name).ToArray();
            var occurencePairs = materialsWithShaderInstance
                .GroupBy(m => m)
                .Select(g => new KeyValuePair<Material, int>(g.Key, g.Count())).ToArray();
            occurencePairs.SortByDescending(x => x.Value);
            return occurencePairs;
        }

        protected override void DrawObjectField()
        {
            using (new eUtility.DisabledGroup())
            {
                EditorGUILayout.LabelField($"{_shader.name}: {_amountOfOccurrences}");
                EditorGUILayout.ObjectField(_shader, typeof(Shader), false);
            }
        }

        protected override void DrawUnfoldedGUI()
        {
            GUILayout.Label("Shader Info", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Amount of passes: ");
            EditorGUILayout.LabelField(_shader.passCount.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Amount of keywords: ");
            EditorGUILayout.LabelField(_shader.keywordSpace.keywordCount.ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Material name", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Amount of uses", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            foreach (var materialUsePair in _shaderUses)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{materialUsePair.Key.name}");
                EditorGUILayout.LabelField($"{materialUsePair.Value}");
                GUILayout.EndHorizontal();
            }
        }
    }
}