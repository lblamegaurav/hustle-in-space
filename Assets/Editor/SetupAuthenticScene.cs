using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceSim.Editor
{
    public static class SetupAuthenticScene
    {
        [MenuItem("Space Sim/Setup Authentic NASA Scene", priority = 1)]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[Space Sim] Configuring authentic NASA 3D assets in scene: {scene.name}...");

            // 1. Remove old test objects if present
            var oldCube = GameObject.Find("Cube");
            if (oldCube != null) Object.DestroyImmediate(oldCube);

            // 2. Setup Saturn V on Pad 39A
            var saturn = GameObject.Find("SaturnV_Apollo11");
            var saturnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/SaturnV_Authentic.fbx");
            if (saturn == null && saturnPrefab != null)
            {
                saturn = (GameObject)PrefabUtility.InstantiatePrefab(saturnPrefab);
                saturn.name = "SaturnV_Apollo11";
            }
            if (saturn != null)
            {
                saturn.transform.position = new Vector3(0f, 5.0f, 0f);
                saturn.transform.rotation = Quaternion.identity;
                saturn.SetActive(true);

                // Rigidbody
                var rb = saturn.GetComponent<Rigidbody>();
                if (rb == null) rb = saturn.AddComponent<Rigidbody>();
                rb.mass = 2970000f;
                rb.isKinematic = true;
                rb.useGravity = false;

                // Flight controller
                var fc = saturn.GetComponent<SaturnV.SaturnVFlightController>();
                if (fc == null) fc = saturn.AddComponent<SaturnV.SaturnVFlightController>();
            }

            // 3. Setup Space Shuttle on Pad 39B (x = 180m, isolated and inactive by default)
            var shuttle = GameObject.Find("SpaceShuttle_Stack");
            var shuttlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/SpaceShuttle_FullStack.fbx");
            if (shuttle == null && shuttlePrefab != null)
            {
                shuttle = (GameObject)PrefabUtility.InstantiatePrefab(shuttlePrefab);
                shuttle.name = "SpaceShuttle_Stack";
            }
            if (shuttle != null)
            {
                shuttle.transform.position = new Vector3(180f, 5.0f, 0f);
                shuttle.transform.rotation = Quaternion.identity;
                shuttle.SetActive(false); // Inactive so it NEVER overlaps with Saturn V!
            }

            // 4. Setup Crawler-Transporter along the crawlerway
            var crawler = GameObject.Find("Crawler_Transporter");
            var crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Crawler.fbx");
            if (crawler == null && crawlerPrefab != null)
            {
                crawler = (GameObject)PrefabUtility.InstantiatePrefab(crawlerPrefab);
                crawler.name = "Crawler_Transporter";
                crawler.transform.position = new Vector3(0f, 0f, -40f);
                crawler.transform.localScale = new Vector3(4.5f, 4.5f, 4.5f);
            }

            // 5. Setup Vehicle Assembly Building (VAB)
            var vab = GameObject.Find("VehicleAssemblyBuilding_VAB");
            var vabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/VAB.fbx");
            if (vab == null && vabPrefab != null)
            {
                vab = (GameObject)PrefabUtility.InstantiatePrefab(vabPrefab);
                vab.name = "VehicleAssemblyBuilding_VAB";
                vab.transform.position = new Vector3(0f, 0f, -250f);
                vab.transform.localScale = new Vector3(10f, 10f, 10f);
            }

            // 6. Setup Complete Environment (Terrain, Cliffs, Trees, Orbital Earth, Moon, Cockpit)
            var envMgr = Object.FindAnyObjectByType<Core.SpaceEnvironmentManager>();
            if (envMgr == null)
            {
                var mgrGo = GameObject.Find("SpaceSim_RuntimeManager");
                if (mgrGo == null) mgrGo = new GameObject("SpaceSim_RuntimeManager");
                envMgr = mgrGo.AddComponent<Core.SpaceEnvironmentManager>();
            }
            if (envMgr != null)
            {
                envMgr.BuildCompleteEnvironment();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Space Sim] Authentic NASA Scene configured and saved successfully!");
        }
    }
}
