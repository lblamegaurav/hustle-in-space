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

            // 5. Ensure Environment Manager (Rocks, Trees, Crawlerway, Earth, Moon, Cockpit)
            var envMgr = FindAnyObjectByType<SpaceEnvironmentManager>();
            if (envMgr == null)
            {
                envMgr = gameObject.AddComponent<SpaceEnvironmentManager>();
            }

            // 6. Ensure 3D Viewport Lighting
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

    /// <summary>
    /// Master manager for the authentic 3D environment surrounding Kennedy Space Center,
    /// the crawlerway, coastal terrain, trees, high-altitude orbital Earth, Moon, and the Apollo cockpit interior.
    /// Uses all authentic models provided in the NASA assets collection.
    /// </summary>
    public class SpaceEnvironmentManager : MonoBehaviour
    {
        public static SpaceEnvironmentManager Instance { get; private set; }

        [Header("Environment Anchors")]
        public Transform environmentRoot;
        public GameObject earthOrbitObject;
        public GameObject moonOrbitObject;
        public GameObject cockpitInteriorObject;
        public GameObject spaceSuitObject;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildCompleteEnvironment();
        }

        public void BuildCompleteEnvironment()
        {
            if (environmentRoot == null)
            {
                var envGo = GameObject.Find("SpaceSim_Environment");
                if (envGo == null) envGo = new GameObject("SpaceSim_Environment");
                environmentRoot = envGo.transform;
            }

            BuildCrawlerwayAndGround();
            PlaceRocksAndCliffs();
            PlaceOakTrees();
            SetupCelestialBodies();
            SetupCockpitInterior();
            SetupSpaceSuitDisplay();
        }

        private void BuildCrawlerwayAndGround()
        {
            var groundGo = GameObject.Find("KSC_GroundTerrain");
            if (groundGo == null)
            {
                groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                groundGo.name = "KSC_GroundTerrain";
                groundGo.transform.SetParent(environmentRoot, false);
                groundGo.transform.position = new Vector3(90f, 0f, -100f);
                groundGo.transform.localScale = new Vector3(80f, 1f, 80f);

                var renderer = groundGo.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    string prop = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    mat.SetColor(prop, new Color(0.18f, 0.20f, 0.19f));
                    renderer.sharedMaterial = mat;
                }
            }

            var crawlerwayGo = GameObject.Find("KSC_Crawlerway");
            if (crawlerwayGo == null)
            {
                crawlerwayGo = new GameObject("KSC_Crawlerway");
                crawlerwayGo.transform.SetParent(environmentRoot, false);

                var track1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                track1.name = "Track_Left";
                track1.transform.SetParent(crawlerwayGo.transform, false);
                track1.transform.position = new Vector3(-8f, 0.15f, -135f);
                track1.transform.localScale = new Vector3(12f, 0.3f, 250f);

                var track2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                track2.name = "Track_Right";
                track2.transform.SetParent(crawlerwayGo.transform, false);
                track2.transform.position = new Vector3(8f, 0.15f, -135f);
                track2.transform.localScale = new Vector3(12f, 0.3f, 250f);

                var gravelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                string gProp = gravelMat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                gravelMat.SetColor(gProp, new Color(0.48f, 0.44f, 0.38f));

                var m1 = track1.GetComponent<MeshRenderer>();
                var m2 = track2.GetComponent<MeshRenderer>();
                if (m1 != null) m1.sharedMaterial = gravelMat;
                if (m2 != null) m2.sharedMaterial = gravelMat;
            }
        }

        private void PlaceRocksAndCliffs()
        {
            var rocksGroup = GameObject.Find("KSC_PerimeterRocks");
            if (rocksGroup != null) return;

            rocksGroup = new GameObject("KSC_PerimeterRocks");
            rocksGroup.transform.SetParent(environmentRoot, false);

            var cliffPrefab = Resources.Load<GameObject>("Models/peak_cliff_mountain_rock");
            var stonePrefab = Resources.Load<GameObject>("Models/stone_rockpart11");

            if (cliffPrefab != null)
            {
                var c1 = Instantiate(cliffPrefab, new Vector3(-280f, 0f, 150f), Quaternion.Euler(0, 45f, 0), rocksGroup.transform);
                c1.name = "CoastCliff_North";
                c1.transform.localScale = Vector3.one * 15f;

                var c2 = Instantiate(cliffPrefab, new Vector3(380f, 0f, 200f), Quaternion.Euler(0, 190f, 0), rocksGroup.transform);
                c2.name = "CoastCliff_East";
                c2.transform.localScale = Vector3.one * 20f;
            }

            if (stonePrefab != null)
            {
                var s1 = Instantiate(stonePrefab, new Vector3(-90f, 0f, 80f), Quaternion.Euler(0, 110f, 0), rocksGroup.transform);
                s1.name = "PerimeterStone_1";
                s1.transform.localScale = Vector3.one * 8f;

                var s2 = Instantiate(stonePrefab, new Vector3(120f, 0f, -80f), Quaternion.Euler(0, 310f, 0), rocksGroup.transform);
                s2.name = "PerimeterStone_2";
                s2.transform.localScale = Vector3.one * 7f;
            }
        }

        private void PlaceOakTrees()
        {
            var treesGroup = GameObject.Find("KSC_Foliage");
            if (treesGroup != null) return;

            treesGroup = new GameObject("KSC_Foliage");
            treesGroup.transform.SetParent(environmentRoot, false);

            var treePrefab = Resources.Load<GameObject>("Models/oaktrees");
            if (treePrefab == null) return;

            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-45f, 0f, -50f),
                new Vector3(-55f, 0f, -90f),
                new Vector3(-50f, 0f, -160f),
                new Vector3(-60f, 0f, -220f),
                new Vector3(45f, 0f, -60f),
                new Vector3(55f, 0f, -120f),
                new Vector3(50f, 0f, -180f),
                new Vector3(65f, 0f, -240f),
                new Vector3(-120f, 0f, 40f),
                new Vector3(140f, 0f, 60f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                var t = Instantiate(treePrefab, treePositions[i], Quaternion.Euler(0, i * 37f, 0), treesGroup.transform);
                t.name = $"OakTreeStand_{i}";
                t.transform.localScale = Vector3.one * Random.Range(3.5f, 5.5f);
            }
        }

        private void SetupCelestialBodies()
        {
            var earthPrefab = Resources.Load<GameObject>("Models/Earth_NASA");
            if (earthPrefab != null && earthOrbitObject == null)
            {
                earthOrbitObject = GameObject.Find("Orbital_Earth_NASA");
                if (earthOrbitObject == null)
                {
                    earthOrbitObject = Instantiate(earthPrefab, new Vector3(0f, -1200f, 0f), Quaternion.Euler(23.5f, 0, 0));
                    earthOrbitObject.name = "Orbital_Earth_NASA";
                    earthOrbitObject.transform.localScale = Vector3.one * 800f;
                    earthOrbitObject.transform.SetParent(environmentRoot, false);
                }
            }

            var moonPrefab = Resources.Load<GameObject>("Models/Moon_NASA");
            if (moonPrefab != null && moonOrbitObject == null)
            {
                moonOrbitObject = GameObject.Find("Translunar_Moon_NASA");
                if (moonOrbitObject == null)
                {
                    moonOrbitObject = Instantiate(moonPrefab, new Vector3(1500f, 2800f, 3200f), Quaternion.identity);
                    moonOrbitObject.name = "Translunar_Moon_NASA";
                    moonOrbitObject.transform.localScale = Vector3.one * 350f;
                    moonOrbitObject.transform.SetParent(environmentRoot, false);
                }
            }
        }

        private void SetupCockpitInterior()
        {
            var cockpitPrefab = Resources.Load<GameObject>("Models/Apollo11_Cockpit_Interior");
            if (cockpitPrefab != null && cockpitInteriorObject == null)
            {
                cockpitInteriorObject = GameObject.Find("Apollo11_Cockpit_Interior");
                if (cockpitInteriorObject == null)
                {
                    var saturn = GameObject.Find("SaturnV_Apollo11");
                    Transform parentTransform = (saturn != null) ? saturn.transform : environmentRoot;

                    cockpitInteriorObject = Instantiate(cockpitPrefab, parentTransform);
                    cockpitInteriorObject.name = "Apollo11_Cockpit_Interior";
                    cockpitInteriorObject.transform.localPosition = new Vector3(0f, 98.8f, 0f);
                    cockpitInteriorObject.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                    cockpitInteriorObject.transform.localScale = Vector3.one * 0.45f;
                }
            }
        }

        private void SetupSpaceSuitDisplay()
        {
            var suitPrefab = Resources.Load<GameObject>("Models/Apollo_A7L_SpaceSuit");
            if (suitPrefab != null && spaceSuitObject == null)
            {
                spaceSuitObject = GameObject.Find("Apollo_A7L_SpaceSuit_Display");
                if (spaceSuitObject == null)
                {
                    spaceSuitObject = Instantiate(suitPrefab, new Vector3(25f, 5.0f, -10f), Quaternion.Euler(0, -45f, 0));
                    spaceSuitObject.name = "Apollo_A7L_SpaceSuit_Display";
                    spaceSuitObject.transform.localScale = Vector3.one * 1.5f;
                    spaceSuitObject.transform.SetParent(environmentRoot, false);
                }
            }
        }
    }
}
