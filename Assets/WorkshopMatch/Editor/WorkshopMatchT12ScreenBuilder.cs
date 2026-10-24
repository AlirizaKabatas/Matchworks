using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorkshopMatch.Editor
{
    public static class WorkshopMatchT12ScreenBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Window/Workshop Match/Create T12 Screen Roots")]
        public static void CreateScreenRoots()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string[] names =
            {
                "MainMenuScreen", "FullMapScreen", "GameplayScreen", "SettingsPopup", "ResultTransition"
            };
            foreach (string rootName in names)
            {
                GameObject existing = GameObject.Find(rootName);
                if (existing != null && existing.scene == scene) continue;
                GameObject root = new(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, $"Create {rootName}");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("T12_SCREEN_ROOTS_READY count=5 scene=SampleScene");
        }
    }
}
