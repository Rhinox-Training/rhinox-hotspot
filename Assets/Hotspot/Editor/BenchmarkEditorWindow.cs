using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotspot.Editor
{
    public class BenchmarkEditorWindow : CustomEditorWindow
    {
        [ReadOnly]
        public SceneAsset CurrentScene;

        private string _benchmarkAssetPath;
        private BenchmarkConfiguration _benchmarkAsset;
        private IOrderedDrawable _listDrawable;
        private IOrderedDrawable _listDrawable2;
        private IOrderedDrawable _dropdownField;
        private float _benchmarkProgress;
        private bool _benchmarkRunning;
        private IBenchmarkStage _currentStage;
        private Benchmark _benchmark;
        private const string BENCHMARKS_FOLDER = "Assets/Editor/Hotspot";


        [MenuItem(HotspotWindowHelper.ANALYSIS_PREFIX + "Benchmark Editor", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(BenchmarkEditorWindow));
            win.titleContent = new GUIContent("Benchmark Editor");
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            UpdateScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.sceneOpened += OnSceneOpened;

        }

        protected override void OnDestroy()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            base.OnDestroy();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            if (_benchmarkAsset == null)
            {
                if (GUILayout.Button("Create Benchmark Asset"))
                {
                    FileHelper.CreateAssetsDirectory(BENCHMARKS_FOLDER);
                    var config = ScriptableObject.CreateInstance<BenchmarkConfiguration>();
                    config.Scene = new SceneReferenceData(CurrentScene);
                    AssetDatabase.CreateAsset(config, _benchmarkAssetPath);
                    _benchmarkAsset = config;
                }
            }
            else
            {
                if (!Application.isPlaying && _benchmarkRunning)
                    _benchmarkRunning = false;
                using (new eUtility.DisabledGroup(_benchmarkRunning))
                    DrawBenchmarkEditor(_benchmarkAsset);
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            using (new eUtility.DisabledGroup(_benchmarkAsset == null || !Application.isPlaying))
            {
                if (GUILayout.Button("Run Benchmark"))
                {
                    _benchmark = new Benchmark(_benchmarkAsset);
                    _benchmark.BenchmarkTick += Repaint;
                    _benchmark.Run();
                }
                DrawBenchmarkProgress();
            }
            
            GUILayout.FlexibleSpace();
        }
        
        private void DrawBenchmarkProgress()
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.DrawRect(rect, CustomGUIStyles.BorderColor);

            var progressRect = rect.AlignLeft(rect.width * _benchmarkProgress);
            EditorGUI.DrawRect(progressRect, Color.gray);


            string progressText = $"{_benchmarkProgress * 100:0.00}%";
            var tempContent = GUIContentHelper.TempContent(progressText);
            float labelWidth = CustomGUIStyles.Label.CalcSize(tempContent).x;
            EditorGUI.LabelField(rect.AlignCenter(labelWidth), tempContent);

        }

        private void DrawBenchmarkEditor(BenchmarkConfiguration benchmarkAsset)
        {
            if (_listDrawable == null)
            {
                var so = new SerializedObject(benchmarkAsset);
                var property = so.FindProperty(nameof(BenchmarkConfiguration.Entries));
                _listDrawable = DrawableFactory.CreateDrawableFor(property);
            }

            if (_dropdownField == null)
            {
                var so = new SerializedObject(benchmarkAsset);
                var property = so.FindProperty(nameof(BenchmarkConfiguration.PoseApplierType));
                _dropdownField = DrawableFactory.CreateDrawableFor(property);
            }
            
            if (_listDrawable2 == null)
            {
                var so = new SerializedObject(benchmarkAsset);
                var property = so.FindProperty(nameof(BenchmarkConfiguration.Statistics));
                _listDrawable2 = DrawableFactory.CreateDrawableFor(property);
            }

            using (new eUtility.PaddedGUIScope(CustomGUIUtility.Padding * 2.0f))
            {
                _dropdownField.Draw(GUIContentHelper.TempContent("PoseApplier"));
                _listDrawable.Draw(GUIContent.none);
                _listDrawable2.Draw(GUIContent.none);
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UpdateScene(scene);
        }

        private void UpdateScene(Scene activeScene)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScene.path);
            CurrentScene = sceneAsset;
            _benchmarkAssetPath = GetAssetPath(sceneAsset);
            _benchmarkAsset = AssetDatabase.LoadAssetAtPath<BenchmarkConfiguration>(_benchmarkAssetPath);
            _listDrawable = null;
        }

        private string GetAssetPath(SceneAsset asset)
        {
            return $"{BENCHMARKS_FOLDER}/{asset.name}.asset";
        }
    }
}
