using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceSim.Models
{
    /// <summary>
    /// Procedural 3D builder for the NASA Space Transportation System (Space Shuttle STS-107 Columbia).
    /// Creates authentic 3D geometry for the External Tank, Twin Solid Rocket Boosters, and
    /// Orbiter Vehicle (wings, cockpit, RS-25 engines, thermal protection system) in Unity URP.
    /// </summary>
    public class SpaceShuttleBuilder : MonoBehaviour
    {
        // Real-world Space Shuttle dimensions (1 unit = 1 meter)
        private const float ET_RADIUS = 4.2f;       // 8.4m diameter
        private const float ET_HEIGHT = 46.9f;
        private const float SRB_RADIUS = 1.85f;     // 3.7m diameter
        private const float SRB_HEIGHT = 45.5f;
        private const float SRB_OFFSET_X = 6.3f;    // Distance from centerline to SRB centers
        private const float ORBITER_LENGTH = 37.2f;
        private const float ORBITER_WINGSPAN = 23.8f;
        private const float ORBITER_OFFSET_Z = 7.0f; // Distance forward of ET centerline

#if UNITY_EDITOR
        [MenuItem("Space Sim/Build Space Shuttle Stack", false, 2)]
        public static void BuildShuttleMenu()
        {
            BuildCompleteShuttle();
        }
#endif

        public static GameObject BuildCompleteShuttle()
        {
            var existing = GameObject.Find("SpaceShuttle_Stack");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            Debug.Log("<color=orange><b>[Space Shuttle]</b></color> Constructing STS-107 Space Shuttle stack...");

            var stackRoot = new GameObject("SpaceShuttle_Stack");
            stackRoot.transform.position = Vector3.zero;

            var materials = CreateShuttleMaterials();

            // 1. External Tank (Centerpiece)
            var etRoot = BuildExternalTank(stackRoot.transform, materials);

            // 2. Twin Solid Rocket Boosters (Left & Right)
            var srbRoot = BuildSolidRocketBoosters(stackRoot.transform, materials);

            // 3. Orbiter Spaceplane (Mounted to ET)
            var orbiterRoot = BuildOrbiterVehicle(stackRoot.transform, materials);

            Debug.Log("<color=green><b>[Space Shuttle]</b></color> STS-107 Shuttle Stack assembly complete!");
            return stackRoot;
        }

        // --- MATERIALS & SHADERS ---
        private static Dictionary<string, Material> CreateShuttleMaterials()
        {
            var mats = new Dictionary<string, Material>();
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            // ET Foam Orange (Polyisocyanurate spray-on foam)
            var etMat = new Material(urpShader) { name = "M_Shuttle_ET_Orange" };
            etMat.color = new Color(0.82f, 0.44f, 0.18f); // Burnt orange
            etMat.SetFloat("_Smoothness", 0.15f);
            mats["ET_Orange"] = etMat;

            // ET Intertank Ribbed Dark Orange
            var etInterMat = new Material(urpShader) { name = "M_Shuttle_ET_Intertank" };
            etInterMat.color = new Color(0.68f, 0.35f, 0.14f);
            etInterMat.SetFloat("_Smoothness", 0.1f);
            mats["ET_Intertank"] = etInterMat;

            // SRB White Casing
            var srbMat = new Material(urpShader) { name = "M_Shuttle_SRB_White" };
            srbMat.color = new Color(0.92f, 0.92f, 0.94f);
            srbMat.SetFloat("_Smoothness", 0.35f);
            mats["SRB_White"] = srbMat;

            // SRB / Nozzle Dark Steel
            var steelMat = new Material(urpShader) { name = "M_Shuttle_Steel" };
            steelMat.color = new Color(0.22f, 0.22f, 0.25f);
            steelMat.SetFloat("_Metallic", 0.6f);
            steelMat.SetFloat("_Smoothness", 0.45f);
            mats["Steel"] = steelMat;

            // Orbiter Upper Fuselage White (Nomex Quilting / LRSI)
            var orbiterWhite = new Material(urpShader) { name = "M_Orbiter_White" };
            orbiterWhite.color = new Color(0.95f, 0.95f, 0.95f);
            orbiterWhite.SetFloat("_Smoothness", 0.4f);
            mats["Orbiter_White"] = orbiterWhite;

            // Orbiter Underbelly & Leading Edge Black (HRSI Silica Tiles & RCC)
            var tpsBlack = new Material(urpShader) { name = "M_Orbiter_TPS_Black" };
            tpsBlack.color = new Color(0.08f, 0.08f, 0.09f);
            tpsBlack.SetFloat("_Smoothness", 0.1f);
            mats["TPS_Black"] = tpsBlack;

            // Cockpit Windshield (Gloss Dark Glass)
            var glassMat = new Material(urpShader) { name = "M_Orbiter_Cockpit_Glass" };
            glassMat.color = new Color(0.04f, 0.06f, 0.08f);
            glassMat.SetFloat("_Metallic", 0.2f);
            glassMat.SetFloat("_Smoothness", 0.95f);
            mats["Cockpit_Glass"] = glassMat;

            // RS-25 Engine Nozzles (Heat-treated Titanium/Inconel)
            var engineMat = new Material(urpShader) { name = "M_RS25_Nozzle" };
            engineMat.color = new Color(0.28f, 0.30f, 0.33f);
            engineMat.SetFloat("_Metallic", 0.8f);
            engineMat.SetFloat("_Smoothness", 0.6f);
            mats["Engine_RS25"] = engineMat;

            return mats;
        }

        // --- EXTERNAL TANK ---
        private static GameObject BuildExternalTank(Transform parent, Dictionary<string, Material> mats)
        {
            var etRoot = new GameObject("ExternalTank_Root");
            etRoot.transform.SetParent(parent, false);

            // Aft LOX Tank Cylinder
            float aftHeight = 22.0f;
            var aftCyl = CreateCylinder("ET_AftTank", ET_RADIUS, ET_RADIUS, aftHeight, mats["ET_Orange"]);
            aftCyl.transform.SetParent(etRoot.transform, false);
            aftCyl.transform.localPosition = new Vector3(0, aftHeight * 0.5f + 4.0f, 0);

            // Intertank Collar (Ribbed section)
            float interHeight = 7.0f;
            var interCollar = CreateCylinder("ET_Intertank", ET_RADIUS * 1.01f, ET_RADIUS * 1.01f, interHeight, mats["ET_Intertank"]);
            interCollar.transform.SetParent(etRoot.transform, false);
            interCollar.transform.localPosition = new Vector3(0, 4.0f + aftHeight + interHeight * 0.5f, 0);

            // Forward LH2 Tank Cylinder
            float fwdHeight = 12.0f;
            var fwdCyl = CreateCylinder("ET_ForwardTank", ET_RADIUS, ET_RADIUS, fwdHeight, mats["ET_Orange"]);
            fwdCyl.transform.SetParent(etRoot.transform, false);
            fwdCyl.transform.localPosition = new Vector3(0, 4.0f + aftHeight + interHeight + fwdHeight * 0.5f, 0);

            // Nose Cone (Ogival Cone)
            float noseHeight = 6.0f;
            var noseCone = CreateCylinder("ET_NoseCone", ET_RADIUS, 0.4f, noseHeight, mats["ET_Orange"]);
            noseCone.transform.SetParent(etRoot.transform, false);
            noseCone.transform.localPosition = new Vector3(0, 4.0f + aftHeight + interHeight + fwdHeight + noseHeight * 0.5f, 0);

            // Aft LOX Feedline Conduit (Runs down the side)
            var feedline = CreateCylinder("ET_LOX_Feedline", 0.35f, 0.35f, 32.0f, mats["ET_Orange"]);
            feedline.transform.SetParent(etRoot.transform, false);
            feedline.transform.localPosition = new Vector3(0, 22.0f, -ET_RADIUS - 0.2f);

            return etRoot;
        }

        // --- SOLID ROCKET BOOSTERS ---
        private static GameObject BuildSolidRocketBoosters(Transform parent, Dictionary<string, Material> mats)
        {
            var srbPair = new GameObject("SRB_Pair");
            srbPair.transform.SetParent(parent, false);

            BuildSingleSRB("SRB_Left", -SRB_OFFSET_X, srbPair.transform, mats);
            BuildSingleSRB("SRB_Right", SRB_OFFSET_X, srbPair.transform, mats);

            return srbPair;
        }

        private static void BuildSingleSRB(string name, float xOffset, Transform parent, Dictionary<string, Material> mats)
        {
            var srbObj = new GameObject(name);
            srbObj.transform.SetParent(parent, false);
            srbObj.transform.localPosition = new Vector3(xOffset, 0, 0);

            // Aft Skirt & Gimbal Ring
            var skirt = CreateCylinder(name + "_AftSkirt", SRB_RADIUS * 1.35f, SRB_RADIUS, 4.0f, mats["Steel"]);
            skirt.transform.SetParent(srbObj.transform, false);
            skirt.transform.localPosition = new Vector3(0, 2.0f, 0);

            // Exhaust Nozzle Bell
            var nozzle = CreateCylinder(name + "_Nozzle", SRB_RADIUS * 0.7f, SRB_RADIUS * 1.25f, 3.5f, mats["Steel"]);
            nozzle.transform.SetParent(srbObj.transform, false);
            nozzle.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Main 4-Segment Motor Body
            float bodyHeight = 32.0f;
            var body = CreateCylinder(name + "_MotorSegments", SRB_RADIUS, SRB_RADIUS, bodyHeight, mats["SRB_White"]);
            body.transform.SetParent(srbObj.transform, false);
            body.transform.localPosition = new Vector3(0, 4.0f + bodyHeight * 0.5f, 0);

            // Forward Casing & Separation Motors Ring
            var fwdRing = CreateCylinder(name + "_ForwardRing", SRB_RADIUS, SRB_RADIUS, 3.5f, mats["Steel"]);
            fwdRing.transform.SetParent(srbObj.transform, false);
            fwdRing.transform.localPosition = new Vector3(0, 4.0f + bodyHeight + 1.75f, 0);

            // Conical Nose Cap
            float coneHeight = 5.0f;
            var nose = CreateCylinder(name + "_NoseCone", SRB_RADIUS, 0.2f, coneHeight, mats["SRB_White"]);
            nose.transform.SetParent(srbObj.transform, false);
            nose.transform.localPosition = new Vector3(0, 4.0f + bodyHeight + 3.5f + coneHeight * 0.5f, 0);
        }

        // --- ORBITER VEHICLE ---
        private static GameObject BuildOrbiterVehicle(Transform parent, Dictionary<string, Material> mats)
        {
            var orbiterRoot = new GameObject("Orbiter_Root");
            orbiterRoot.transform.SetParent(parent, false);
            orbiterRoot.transform.localPosition = new Vector3(0, 6.0f, ORBITER_OFFSET_Z);

            // 1. Main Fuselage
            var fuselage = new GameObject("Fuselage");
            fuselage.transform.SetParent(orbiterRoot.transform, false);

            var cabinBody = CreateBox("Cabin_Body", new Vector3(5.2f, 4.8f, 26.0f), mats["Orbiter_White"]);
            cabinBody.transform.SetParent(fuselage.transform, false);
            cabinBody.transform.localPosition = new Vector3(0, 2.4f, 10.0f);

            // Nose Forward Section & Cap (Black RCC)
            var noseCap = CreateCylinder("Nose_RCC_Cap", 2.2f, 0.5f, 4.0f, mats["TPS_Black"]);
            noseCap.transform.SetParent(fuselage.transform, false);
            noseCap.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            noseCap.transform.localPosition = new Vector3(0, 2.0f, 25.0f);

            // 2. Cockpit & Windshield
            var cockpit = new GameObject("Cockpit_Avionics");
            cockpit.transform.SetParent(orbiterRoot.transform, false);

            var windshield = CreateBox("Windshield_Windows", new Vector3(3.4f, 1.2f, 2.8f), mats["Cockpit_Glass"]);
            windshield.transform.SetParent(cockpit.transform, false);
            windshield.transform.localPosition = new Vector3(0, 4.2f, 20.5f);
            windshield.transform.localRotation = Quaternion.Euler(-25f, 0, 0);

            // 3. Double-Delta Wings & Underbelly TPS Heat Shield
            var tps = new GameObject("TPS_HeatShield");
            tps.transform.SetParent(orbiterRoot.transform, false);

            // Left Wing
            var leftWing = CreateBox("Wing_Left", new Vector3(ORBITER_WINGSPAN * 0.45f, 0.45f, 14.0f), mats["TPS_Black"]);
            leftWing.transform.SetParent(tps.transform, false);
            leftWing.transform.localPosition = new Vector3(-ORBITER_WINGSPAN * 0.28f, 0.3f, 7.0f);
            leftWing.transform.localRotation = Quaternion.Euler(0, 20f, 3f);

            // Right Wing
            var rightWing = CreateBox("Wing_Right", new Vector3(ORBITER_WINGSPAN * 0.45f, 0.45f, 14.0f), mats["TPS_Black"]);
            rightWing.transform.SetParent(tps.transform, false);
            rightWing.transform.localPosition = new Vector3(ORBITER_WINGSPAN * 0.28f, 0.3f, 7.0f);
            rightWing.transform.localRotation = Quaternion.Euler(0, -20f, -3f);

            // Underbelly Heat Tiles Mosaic
            var bellyTile = CreateBox("Underbelly_Tiles", new Vector3(5.4f, 0.3f, 28.0f), mats["TPS_Black"]);
            bellyTile.transform.SetParent(tps.transform, false);
            bellyTile.transform.localPosition = new Vector3(0, 0.1f, 10.0f);

            // 4. Payload Bay
            var payloadBay = new GameObject("PayloadBay_Root");
            payloadBay.transform.SetParent(orbiterRoot.transform, false);

            var bayDoors = CreateCylinder("Bay_RadiatorDoors", 2.6f, 2.6f, 16.0f, mats["Orbiter_White"]);
            bayDoors.transform.SetParent(payloadBay.transform, false);
            bayDoors.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            bayDoors.transform.localPosition = new Vector3(0, 3.6f, 10.0f);

            // 5. Vertical Stabilizer (Tail Fin)
            var vertTail = CreateBox("Vertical_Tail_Fin", new Vector3(0.5f, 8.5f, 6.0f), mats["Orbiter_White"]);
            vertTail.transform.SetParent(orbiterRoot.transform, false);
            vertTail.transform.localPosition = new Vector3(0, 8.2f, 0.0f);
            vertTail.transform.localRotation = Quaternion.Euler(-18f, 0, 0);

            // 6. OMS Propulsion Pods (Left & Right)
            var leftOms = CreateCylinder("OMS_Pod_Left", 1.4f, 0.9f, 6.0f, mats["Orbiter_White"]);
            leftOms.transform.SetParent(orbiterRoot.transform, false);
            leftOms.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            leftOms.transform.localPosition = new Vector3(-2.2f, 4.0f, -0.5f);

            var rightOms = CreateCylinder("OMS_Pod_Right", 1.4f, 0.9f, 6.0f, mats["Orbiter_White"]);
            rightOms.transform.SetParent(orbiterRoot.transform, false);
            rightOms.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            rightOms.transform.localPosition = new Vector3(2.2f, 4.0f, -0.5f);

            // 7. RS-25 Main Engines (Triangle cluster)
            var ssmeRoot = new GameObject("RS25_Engines");
            ssmeRoot.transform.SetParent(orbiterRoot.transform, false);

            // Top Center SSME #1
            var engine1 = CreateCylinder("RS25_TopCenter", 0.6f, 1.25f, 3.2f, mats["Engine_RS25"]);
            engine1.transform.SetParent(ssmeRoot.transform, false);
            engine1.transform.localRotation = Quaternion.Euler(-100f, 0, 0);
            engine1.transform.localPosition = new Vector3(0, 4.4f, -2.5f);

            // Bottom Left SSME #2
            var engine2 = CreateCylinder("RS25_BottomLeft", 0.6f, 1.25f, 3.2f, mats["Engine_RS25"]);
            engine2.transform.SetParent(ssmeRoot.transform, false);
            engine2.transform.localRotation = Quaternion.Euler(-95f, -8f, 0);
            engine2.transform.localPosition = new Vector3(-1.6f, 2.0f, -2.5f);

            // Bottom Right SSME #3
            var engine3 = CreateCylinder("RS25_BottomRight", 0.6f, 1.25f, 3.2f, mats["Engine_RS25"]);
            engine3.transform.SetParent(ssmeRoot.transform, false);
            engine3.transform.localRotation = Quaternion.Euler(-95f, 8f, 0);
            engine3.transform.localPosition = new Vector3(1.6f, 2.0f, -2.5f);

            return orbiterRoot;
        }

        // --- PROCEDURAL PRIMITIVE HELPERS ---
        private static GameObject CreateCylinder(string name, float rBottom, float rTop, float height, Material mat)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;

            mf.sharedMesh = GenerateFrustumMesh(rBottom, rTop, height, 32);
            return go;
        }

        private static GameObject CreateBox(string name, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = mat;
            return go;
        }

        private static Mesh GenerateFrustumMesh(float rBottom, float rTop, float height, int segments)
        {
            var mesh = new Mesh { name = $"Frustum_{rBottom}_{rTop}" };
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            float halfH = height * 0.5f;

            // Side vertices
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                var vBottom = new Vector3(cos * rBottom, -halfH, sin * rBottom);
                var vTop = new Vector3(cos * rTop, halfH, sin * rTop);

                vertices.Add(vBottom);
                vertices.Add(vTop);

                var normal = new Vector3(cos, (rBottom - rTop) / height, sin).normalized;
                normals.Add(normal);
                normals.Add(normal);

                float u = (float)i / segments;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;

                triangles.Add(b0);
                triangles.Add(t0);
                triangles.Add(b1);

                triangles.Add(b1);
                triangles.Add(t0);
                triangles.Add(t1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
