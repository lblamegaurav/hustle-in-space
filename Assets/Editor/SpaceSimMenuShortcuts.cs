using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpaceSim.Models;
using SpaceSim.UI;

namespace SpaceSim.Editor
{
    public static class SpaceSimMenuShortcuts
    {
        [MenuItem("Space Sim/▶ Start Simulation (Play) %#g", priority = 1)]
        public static void StartSimulation()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Ensure SampleScene is active
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }

            EditorApplication.isPlaying = true;
        }

        [MenuItem("Space Sim/Open Main Scene", priority = 2)]
        public static void OpenMainScene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        [MenuItem("Space Sim/Reset First-Time Tutorial Flag", priority = 20)]
        public static void ResetTutorialFlag()
        {
            PlayerPrefs.DeleteKey(GameLoadingManager.TutorialSeenPrefKey);
            PlayerPrefs.Save();
            Debug.Log("<color=cyan><b>[Space Sim]</b></color> First-time tutorial flag reset. The onboarding modal will show on next launch.");
        }
    }
}
