using UnityEngine;
using SpaceSim.Models;
using SpaceSim.UI;

namespace SpaceSim.Core
{
    /// <summary>
    /// Automatic runtime bootstrapper for the Space Simulation suite.
    /// Runs on scene load, orchestrating the 3D models, camera, audio, and UI managers.
    /// Ensures zero missing component setup is required in the Unity Editor.
    /// </summary>
    public class SimulationBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            var existing = FindAnyObjectByType<SimulationBootstrapper>();
            if (existing != null) return;

            var managerGo = new GameObject("SpaceSim_RuntimeManager");
            managerGo.AddComponent<SimulationBootstrapper>();
        }

        private void Awake()
        {
            // 1. Ensure Audio Manager
            var audio = SpaceSimAudio.Instance;

            // 2. Setup Camera
            SetupCamera();

            // 3. Ensure Mission Palette Manager
            var paletteMgr = FindAnyObjectByType<MissionPaletteManager>();
            if (paletteMgr == null)
            {
                paletteMgr = gameObject.AddComponent<MissionPaletteManager>();
            }

            // 4. Ensure Game Loading & Splash Manager
            var loadingMgr = FindAnyObjectByType<GameLoadingManager>();
            if (loadingMgr == null)
            {
                loadingMgr = gameObject.AddComponent<GameLoadingManager>();
            }

            // 5. Ensure 3D Viewport Lighting
            SetupLighting();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.025f, 0.035f); // Deep space dark
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 5000f;

            var orbit = cam.GetComponent<BlenderOrbitCamera>();
            if (orbit == null)
            {
                orbit = cam.gameObject.AddComponent<BlenderOrbitCamera>();
            }
        }

        private void SetupLighting()
        {
            var lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            bool hasDirLight = false;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    hasDirLight = true;
                    l.intensity = 1.4f;
                    l.color = new Color(0.95f, 0.95f, 1.0f);
                    break;
                }
            }

            if (!hasDirLight)
            {
                var lightGo = new GameObject("Directional Light");
                var dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.intensity = 1.4f;
                dirLight.color = new Color(0.95f, 0.95f, 1.0f);
                lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0);
            }
        }
    }
}
