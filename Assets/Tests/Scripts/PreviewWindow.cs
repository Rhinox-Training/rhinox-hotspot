using Tests.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PreviewWindow : SceneView
{
    public CustomPreviewStage Stage;
    public Scene SceneLoaded;

    public static void ShowWindow()
    {
        PreviewWindow window = CreateWindow<PreviewWindow>();

        window.drawGizmos = false;
        
        scene = EditorSceneManager.NewPreviewScene();
        window.SceneLoaded = scene;
        window.SceneLoaded.name = window.name;
        window.customScene = window.SceneLoaded;
        
        window.SetupWindow();
        window.Repaint();


    }

    private static Scene scene;

    private void SetupWindow()
    {
        titleContent = new GUIContent()
        {
            text = "Preview"
        };

        var stage = ScriptableObject.CreateInstance<CustomPreviewStage>();
        stage.OwnerWindow = this;
        StageUtility.GoToStage(stage,true);
        stage.SetupScene();
        
    }
}