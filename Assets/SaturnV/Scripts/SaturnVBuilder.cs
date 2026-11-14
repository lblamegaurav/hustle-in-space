using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaturnV
{
    public class SaturnVBuilder : MonoBehaviour
    {
        // Dimensions matching Saturn V (1 unit = 1 meter)
        private const float R_SIC = 5.0f;       // S-IC radius (10.0m dia)
        private const float R_SIVB = 3.3f;      // S-IVB radius (6.6m dia)
        private const float R_CSM = 1.95f;      // CSM radius (3.9m dia)
        private const float R_APEX = 0.45f;     // CM apex radius

        private const float H_PAD = 5.0f;       // Pad top elevation
        private const float H_SIC = 42.0f;      // S-IC stage height
        private const float H_SII = 24.8f;      // S-II stage height
        private const float H_SIVB = 18.0f;     // S-IVB stage height
        private const float H_SLA = 8.5f;       // SLA conical adapter height
        private const float H_SM = 4.5f;        // Service Module height
        private const float H_CM = 3.2f;        // Command Module height
        private const float H_LES = 12.0f;      // Launch Escape System height

#if UNITY_EDITOR
        [MenuItem("Saturn V/Build Saturn V Rocket", false, 1)]
        public static void BuildRocketMenu()
        {
            BuildCompleteRocket();
        }
#endif

        public static GameObject BuildCompleteRocket()
        {
            // Clean up any existing Saturn V in scene
            var existing = GameObject.Find("SaturnV_Apollo11");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            var padExisting = GameObject.Find("SaturnV_LaunchComplex");
            if (padExisting != null)
            {
                DestroyImmediate(padExisting);
            }

            Debug.Log("<color=cyan><b>[Saturn V]</b></color> Starting construction of Saturn V & Launch Complex 39A...");

            // 1. Create Materials & Textures
            var materials = CreateRocketMaterials();

            // 2. Build Mobile Launcher Platform & Umbilical Tower
            var padRoot = BuildLaunchComplex(materials);

            // 3. Build Rocket Root
            var rocketRoot = new GameObject("SaturnV_Apollo11");
            rocketRoot.transform.position = Vector3.zero;

            var flightCtrl = rocketRoot.AddComponent<SaturnVFlightController>();

            // Current vertical stack cursor (Y)
            float currentY = H_PAD;

            // --- STAGE 1: S-IC ---
            var sicObj = new GameObject("Stage1_S-IC");
            sicObj.transform.parent = rocketRoot.transform;
            sicObj.transform.localPosition = Vector3.zero;

            BuildSICStage(sicObj, materials, currentY, flightCtrl);
            currentY += H_SIC;

            // --- STAGE 2: S-II ---
            var siiObj = new GameObject("Stage2_S-II");
            siiObj.transform.parent = rocketRoot.transform;
            siiObj.transform.localPosition = Vector3.zero;

            BuildSIIStage(siiObj, materials, currentY);
            currentY += H_SII;

            // --- STAGE 3: S-IVB ---
            var sivbObj = new GameObject("Stage3_S-IVB");
            sivbObj.transform.parent = rocketRoot.transform;
            sivbObj.transform.localPosition = Vector3.zero;

            BuildSIVBStage(sivbObj, materials, currentY);
            currentY += H_SIVB;

            // --- SPACECRAFT-LM ADAPTER (SLA) ---
            var slaObj = new GameObject("Spacecraft_LM_Adapter");
            slaObj.transform.parent = rocketRoot.transform;
            slaObj.transform.localPosition = Vector3.zero;

            BuildSLAStage(slaObj, materials, currentY);
            currentY += H_SLA;

            // --- APOLLO CSM (Service & Command Module) ---
            var csmObj = new GameObject("Apollo_CSM");
            csmObj.transform.parent = rocketRoot.transform;
            csmObj.transform.localPosition = Vector3.zero;

            BuildCSM(csmObj, materials, currentY);
            currentY += (H_SM + H_CM);

            // --- LAUNCH ESCAPE SYSTEM (LES) ---
            var lesObj = new GameObject("Launch_Escape_System");
            lesObj.transform.parent = rocketRoot.transform;
            lesObj.transform.localPosition = Vector3.zero;

            BuildLES(lesObj, materials, currentY);
            currentY += H_LES;

            // Total height logged
            Debug.Log(string.Format("<color=green><b>[Saturn V]</b></color> Saturn V assembled successfully! Total height: {0:F1}m (pad to tip: {1:F1}m)", currentY - H_PAD, currentY));

            // Setup Cameras
            SetupCameras(rocketRoot, flightCtrl);

            // Frame scene view on the rocket
#if UNITY_EDITOR
            Selection.activeGameObject = rocketRoot;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
#endif

            return rocketRoot;
        }

        // ==========================================
        // STAGE 1: S-IC (5x F-1 Engines, Fins, Roll Pattern)
        // ==========================================
        private static void BuildSICStage(GameObject parent, Dictionary<string, Material> mats, float baseY, SaturnVFlightController flightCtrl)
        {
            // 1. Thrust Structure / Base Skirt (H: 5m, R: 5.0m)
            var baseSkirt = CreateMeshObject("S-IC_ThrustStructure", CreateFrustum(R_SIC * 1.02f, R_SIC, 5.0f, 36, true, false), mats["MetalDark"], parent.transform);
            baseSkirt.transform.localPosition = new Vector3(0, baseY, 0);

            // 2. Five F-1 Rocket Engines
            float engineY = baseY + 0.5f;
            float outerRadius = 3.2f;

            // Center engine
            CreateF1Engine("F1_Center", parent.transform, new Vector3(0, engineY, 0), mats, flightCtrl);

            // 4 Outer gimbaled engines
            Vector3[] outerPositions = new Vector3[]
            {
                new Vector3( outerRadius, engineY, 0),
                new Vector3(-outerRadius, engineY, 0),
                new Vector3(0, engineY,  outerRadius),
                new Vector3(0, engineY, -outerRadius)
            };

            for (int i = 0; i < outerPositions.Length; i++)
            {
                CreateF1Engine("F1_Outer_" + (i + 1), parent.transform, outerPositions[i], mats, flightCtrl);
            }

            // 3. Four Aerodynamic Engine Fairings and Fins (90-degree spacing)
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Quaternion rot = Quaternion.Euler(0, angle, 0);

                // Engine fairing
                var fairing = CreateMeshObject("Fairing_" + i, CreateFrustum(1.4f, 0.4f, 6.0f, 16, true, true), mats["White"], parent.transform);
                fairing.transform.localPosition = new Vector3(0, baseY, 0) + rot * new Vector3(R_SIC * 0.95f, 0, 0);
                fairing.transform.localRotation = rot * Quaternion.Euler(0, 0, -8f);

                // Fin (Titanium-faced delta fin: root 4m, span 3.5m, thickness 0.15m)
                var fin = CreateMeshObject("AerodynamicFin_" + i, CreateFinMesh(5.5f, 1.8f, 3.8f, 0.16f, 2.2f), mats["White"], parent.transform);
                fin.transform.localPosition = new Vector3(0, baseY + 1.2f, 0) + rot * new Vector3(R_SIC * 0.9f, 0, 0);
                fin.transform.localRotation = rot;
            }

            // 4. Lower White Section (H: 8m)
            var lowerTank = CreateMeshObject("S-IC_LowerTank", CreateFrustum(R_SIC, R_SIC, 8.0f, 36, false, false), mats["White"], parent.transform);
            lowerTank.transform.localPosition = new Vector3(0, baseY + 5.0f, 0);

            // 5. Iconic Roll Pattern Section (H: 18m, 4 alternating black/white quadrants)
            var rollTank = CreateMeshObject("S-IC_RollPatternTank", CreateFrustum(R_SIC, R_SIC, 18.0f, 36, false, false), mats["SIC_RollPattern"], parent.transform);
            rollTank.transform.localPosition = new Vector3(0, baseY + 13.0f, 0);

            // 6. Upper White Section (H: 8m)
            var upperTank = CreateMeshObject("S-IC_UpperTank", CreateFrustum(R_SIC, R_SIC, 8.0f, 36, false, false), mats["White"], parent.transform);
            upperTank.transform.localPosition = new Vector3(0, baseY + 31.0f, 0);

            // 7. S-IC / S-II Forward Skirt & Interstage (H: 3.0m) with 8 retro rockets
            var fwdSkirt = CreateMeshObject("S-IC_ForwardSkirt", CreateFrustum(R_SIC, R_SIC, 3.0f, 36, false, true), mats["White"], parent.transform);
            fwdSkirt.transform.localPosition = new Vector3(0, baseY + 39.0f, 0);

            // Retro rocket blister packs
            for (int r = 0; r < 8; r++)
            {
                float rAngle = r * 45f;
                Quaternion rRot = Quaternion.Euler(0, rAngle, 0);
                var retro = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                retro.name = "RetroRocket_" + r;
                retro.transform.parent = parent.transform;
                retro.transform.localPosition = new Vector3(0, baseY + 40.5f, 0) + rRot * new Vector3(R_SIC + 0.12f, 0, 0);
                retro.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
                retro.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
                DestroyImmediate(retro.GetComponent<Collider>());
            }

            // 8. External LOX Feed Tunnels (2 long vertical half-pipes on opposite sides)
            for (int t = 0; t < 2; t++)
            {
                float tAngle = t * 180f + 45f;
                Quaternion tRot = Quaternion.Euler(0, tAngle, 0);
                var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tunnel.name = "LOX_Tunnel_" + t;
                tunnel.transform.parent = parent.transform;
                tunnel.transform.localPosition = new Vector3(0, baseY + 23.5f, 0) + tRot * new Vector3(R_SIC + 0.2f, 0, 0);
                tunnel.transform.localRotation = tRot;
                tunnel.transform.localScale = new Vector3(0.4f, 34.0f, 0.6f);
                tunnel.GetComponent<Renderer>().sharedMaterial = mats["White"];
                DestroyImmediate(tunnel.GetComponent<Collider>());
            }
        }

        private static void CreateF1Engine(string name, Transform parent, Vector3 localPos, Dictionary<string, Material> mats, SaturnVFlightController flightCtrl)
        {
            var engineGroup = new GameObject(name);
            engineGroup.transform.parent = parent;
            engineGroup.transform.localPosition = localPos;

            // Flared F-1 Bell Nozzle (throat R: 0.65m, exit R: 1.85m, height: 3.4m)
            var nozzle = CreateMeshObject("NozzleBell", CreateFrustum(1.85f, 0.65f, 3.4f, 24, false, false), mats["EngineF1"], engineGroup.transform);
            nozzle.transform.localPosition = new Vector3(0, -3.2f, 0);

            // Gimbal manifold dome
            var manifold = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            manifold.name = "GimbalManifold";
            manifold.transform.parent = engineGroup.transform;
            manifold.transform.localPosition = new Vector3(0, 0.1f, 0);
            manifold.transform.localScale = new Vector3(1.3f, 1.0f, 1.3f);
            manifold.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
            DestroyImmediate(manifold.GetComponent<Collider>());

            // Exhaust gas generator manifold pipe wrapping around nozzle
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ManifoldRing";
            ring.transform.parent = engineGroup.transform;
            ring.transform.localPosition = new Vector3(0, -1.2f, 0);
            ring.transform.localScale = new Vector3(2.3f, 0.15f, 2.3f);
            ring.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
            DestroyImmediate(ring.GetComponent<Collider>());

            // Rocket flame particle emitter & dynamic light at engine exit
            var emitterObj = new GameObject("FlameEmitter");
            emitterObj.transform.parent = engineGroup.transform;
            emitterObj.transform.localPosition = new Vector3(0, -3.2f, 0);
            emitterObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            var ps = emitterObj.AddComponent<ParticleSystem>();
            var psr = emitterObj.GetComponent<ParticleSystemRenderer>();
            ConfigureEngineParticleSystem(ps, psr, mats["Flame"]);
            flightCtrl.engineFlames.Add(ps);

            // Engine point light
            var lightObj = new GameObject("EngineLight");
            lightObj.transform.parent = engineGroup.transform;
            lightObj.transform.localPosition = new Vector3(0, -3.8f, 0);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.55f, 0.15f);
            l.range = 35f;
            l.intensity = 0f;
            flightCtrl.engineLights.Add(l);
        }

        // ==========================================
        // STAGE 2: S-II (5x J-2 Engines, Interstage)
        // ==========================================
        private static void BuildSIIStage(GameObject parent, Dictionary<string, Material> mats, float baseY)
        {
            // 1. S-II Aft Interstage Ring (Corrugated black band, H: 5.5m)
            var interstage = CreateMeshObject("S-II_AftInterstage", CreateFrustum(R_SIC, R_SIC, 5.5f, 36, false, false), mats["BlackBand"], parent.transform);
            interstage.transform.localPosition = new Vector3(0, baseY, 0);

            // 5x J-2 Engines inside interstage
            float j2Y = baseY + 0.8f;
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = (i == 0) ? new Vector3(0, j2Y, 0) :
                    Quaternion.Euler(0, (i - 1) * 90f + 45f, 0) * new Vector3(2.0f, j2Y, 0);

                var j2 = CreateMeshObject("J2_Engine_" + i, CreateFrustum(0.95f, 0.35f, 2.2f, 18, false, false), mats["MetalSilver"], parent.transform);
                j2.transform.localPosition = pos;
            }

            // 2. Main S-II Liquid Hydrogen/LOX Tank (H: 17.5m, White with roll markings)
            var tank = CreateMeshObject("S-II_MainTank", CreateFrustum(R_SIC, R_SIC, 17.5f, 36, false, false), mats["SII_Markings"], parent.transform);
            tank.transform.localPosition = new Vector3(0, baseY + 5.5f, 0);

            // 3. S-II Forward Skirt (H: 1.8m)
            var skirt = CreateMeshObject("S-II_ForwardSkirt", CreateFrustum(R_SIC, R_SIC, 1.8f, 36, false, true), mats["White"], parent.transform);
            skirt.transform.localPosition = new Vector3(0, baseY + 23.0f, 0);

            // 4. Ullage Motor Fairings (4 small wedge fairings)
            for (int u = 0; u < 4; u++)
            {
                float uAngle = u * 90f + 22.5f;
                Quaternion uRot = Quaternion.Euler(0, uAngle, 0);
                var ullage = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ullage.name = "UllageFairing_" + u;
                ullage.transform.parent = parent.transform;
                ullage.transform.localPosition = new Vector3(0, baseY + 5.2f, 0) + uRot * new Vector3(R_SIC + 0.15f, 0, 0);
                ullage.transform.localRotation = uRot;
                ullage.transform.localScale = new Vector3(0.5f, 1.8f, 0.35f);
                ullage.GetComponent<Renderer>().sharedMaterial = mats["White"];
                DestroyImmediate(ullage.GetComponent<Collider>());
            }
        }

        // ==========================================
        // STAGE 3: S-IVB (Third Stage, APS, IU Ring)
        // ==========================================
        private static void BuildSIVBStage(GameObject parent, Dictionary<string, Material> mats, float baseY)
        {
            // 1. S-II to S-IVB Interstage Adapter (Conical frustum from 10m to 6.6m dia, H: 5.5m)
            var adapter = CreateMeshObject("S-IVB_InterstageAdapter", CreateFrustum(R_SIC, R_SIVB, 5.5f, 36, false, false), mats["SIVB_Markings"], parent.transform);
            adapter.transform.localPosition = new Vector3(0, baseY, 0);

            // Single J-2 engine inside adapter
            var j2 = CreateMeshObject("S-IVB_J2_Engine", CreateFrustum(1.1f, 0.4f, 2.5f, 18, false, false), mats["MetalSilver"], parent.transform);
            j2.transform.localPosition = new Vector3(0, baseY + 1.2f, 0);

            // 2. S-IVB Main Tank (H: 11.5m, R: 3.3m, White with vertical roll band)
            var tank = CreateMeshObject("S-IVB_MainTank", CreateFrustum(R_SIVB, R_SIVB, 11.5f, 32, false, false), mats["SIVB_Markings"], parent.transform);
            tank.transform.localPosition = new Vector3(0, baseY + 5.5f, 0);

            // 3. Auxiliary Propulsion System (APS) modules (2 pods on opposite sides)
            for (int a = 0; a < 2; a++)
            {
                float aAngle = a * 180f;
                Quaternion aRot = Quaternion.Euler(0, aAngle, 0);
                var aps = GameObject.CreatePrimitive(PrimitiveType.Cube);
                aps.name = "APS_Pod_" + a;
                aps.transform.parent = parent.transform;
                aps.transform.localPosition = new Vector3(0, baseY + 7.5f, 0) + aRot * new Vector3(R_SIVB + 0.22f, 0, 0);
                aps.transform.localRotation = aRot;
                aps.transform.localScale = new Vector3(0.45f, 1.8f, 0.6f);
                aps.GetComponent<Renderer>().sharedMaterial = mats["White"];
                DestroyImmediate(aps.GetComponent<Collider>());
            }

            // 4. Instrument Unit (IU) Ring (The "Brain" of Saturn V, H: 1.0m, R: 3.3m)
            var iu = CreateMeshObject("InstrumentUnit_IU", CreateFrustum(R_SIVB, R_SIVB, 1.0f, 32, false, true), mats["White"], parent.transform);
            iu.transform.localPosition = new Vector3(0, baseY + 17.0f, 0);
        }

        // ==========================================
        // SPACECRAFT-LM ADAPTER (SLA) & LUNAR MODULE
        // ==========================================
        private static void BuildSLAStage(GameObject parent, Dictionary<string, Material> mats, float baseY)
        {
            // SLA Conical Adapter (tapers from R: 3.3m to R: 1.95m, H: 8.5m)
            var sla = CreateMeshObject("SLA_Fairing", CreateFrustum(R_SIVB, R_CSM, H_SLA, 32, false, false), mats["White"], parent.transform);
            sla.transform.localPosition = new Vector3(0, baseY, 0);

            // 4 Petal separation seams
            for (int p = 0; p < 4; p++)
            {
                float pAngle = p * 90f;
                Quaternion pRot = Quaternion.Euler(0, pAngle, 0);
                var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seam.name = "SLA_Seam_" + p;
                seam.transform.parent = parent.transform;
                seam.transform.localPosition = new Vector3(0, baseY + H_SLA * 0.5f, 0) + pRot * new Vector3(R_SIVB * 0.8f, 0, 0);
                seam.transform.localRotation = pRot;
                seam.transform.localScale = new Vector3(0.06f, H_SLA, 0.06f);
                seam.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
                DestroyImmediate(seam.GetComponent<Collider>());
            }

            // Abstract Lunar Module (LM) inside SLA
            var lmDescent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lmDescent.name = "LunarModule_DescentStage";
            lmDescent.transform.parent = parent.transform;
            lmDescent.transform.localPosition = new Vector3(0, baseY + 2.5f, 0);
            lmDescent.transform.localScale = new Vector3(3.2f, 1.8f, 3.2f);
            lmDescent.GetComponent<Renderer>().sharedMaterial = mats["GoldFoil"];
            DestroyImmediate(lmDescent.GetComponent<Collider>());

            var lmAscent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lmAscent.name = "LunarModule_AscentStage";
            lmAscent.transform.parent = parent.transform;
            lmAscent.transform.localPosition = new Vector3(0, baseY + 4.2f, 0);
            lmAscent.transform.localScale = new Vector3(2.4f, 1.9f, 2.4f);
            lmAscent.GetComponent<Renderer>().sharedMaterial = mats["MetalSilver"];
            DestroyImmediate(lmAscent.GetComponent<Collider>());
        }

        // ==========================================
        // APOLLO CSM (Service & Command Module)
        // ==========================================
        private static void BuildCSM(GameObject parent, Dictionary<string, Material> mats, float baseY)
        {
            // 1. Service Module (SM) Cylinder (R: 1.95m, H: 4.5m)
            var sm = CreateMeshObject("Apollo_ServiceModule", CreateFrustum(R_CSM, R_CSM, H_SM, 28, true, true), mats["MetalSilver"], parent.transform);
            sm.transform.localPosition = new Vector3(0, baseY, 0);

            // SPS Engine Bell under SM
            var sps = CreateMeshObject("SPS_EngineBell", CreateFrustum(0.85f, 0.25f, 1.6f, 16, false, false), mats["MetalDark"], parent.transform);
            sps.transform.localPosition = new Vector3(0, baseY - 1.2f, 0);

            // 4 Quad RCS Thrusters at 90-degree intervals
            for (int q = 0; q < 4; q++)
            {
                float qAngle = q * 90f + 45f;
                Quaternion qRot = Quaternion.Euler(0, qAngle, 0);
                var rcsBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rcsBlock.name = "RCS_Quad_" + q;
                rcsBlock.transform.parent = parent.transform;
                rcsBlock.transform.localPosition = new Vector3(0, baseY + 2.8f, 0) + qRot * new Vector3(R_CSM + 0.12f, 0, 0);
                rcsBlock.transform.localRotation = qRot;
                rcsBlock.transform.localScale = new Vector3(0.2f, 0.25f, 0.25f);
                rcsBlock.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
                DestroyImmediate(rcsBlock.GetComponent<Collider>());
            }

            // 2. Command Module (CM - Apollo Capsule Cone, R_base: 1.95m, R_top: 0.45m, H: 3.2m)
            var cm = CreateMeshObject("Apollo_CommandModule", CreateFrustum(R_CSM, R_APEX, H_CM, 28, true, true), mats["CommandModule"], parent.transform);
            cm.transform.localPosition = new Vector3(0, baseY + H_SM, 0);

            // Heat shield base ring
            var heatshield = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            heatshield.name = "AblativeHeatShield";
            heatshield.transform.parent = parent.transform;
            heatshield.transform.localPosition = new Vector3(0, baseY + H_SM + 0.1f, 0);
            heatshield.transform.localScale = new Vector3(R_CSM * 2.05f, 0.1f, R_CSM * 2.05f);
            heatshield.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
            DestroyImmediate(heatshield.GetComponent<Collider>());

            // CM Docking probe ring at apex
            var dockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dockRing.name = "DockingProbeRing";
            dockRing.transform.parent = parent.transform;
            dockRing.transform.localPosition = new Vector3(0, baseY + H_SM + H_CM, 0);
            dockRing.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);
            dockRing.GetComponent<Renderer>().sharedMaterial = mats["MetalSilver"];
            DestroyImmediate(dockRing.GetComponent<Collider>());
        }

        // ==========================================
        // LAUNCH ESCAPE SYSTEM (LES Tower & Spike)
        // ==========================================
        private static void BuildLES(GameObject parent, Dictionary<string, Material> mats, float baseY)
        {
            // 1. 4-Leg Lattice Escape Tower (Truss structure, H: 4.8m)
            var towerObj = new GameObject("LES_TrussTower");
            towerObj.transform.parent = parent.transform;
            towerObj.transform.localPosition = new Vector3(0, baseY, 0);

            for (int leg = 0; leg < 4; leg++)
            {
                float legAngle = leg * 90f + 45f;
                Quaternion legRot = Quaternion.Euler(0, legAngle, 0);
                Vector3 bottomPos = legRot * new Vector3(R_APEX * 0.9f, 0, 0);
                Vector3 topPos = legRot * new Vector3(0.35f, 4.8f, 0);

                var strut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strut.name = "TrussLeg_" + leg;
                strut.transform.parent = towerObj.transform;
                strut.transform.localPosition = (bottomPos + topPos) * 0.5f;
                strut.transform.up = (topPos - bottomPos).normalized;
                strut.transform.localScale = new Vector3(0.08f, Vector3.Distance(bottomPos, topPos) * 0.5f, 0.08f);
                strut.GetComponent<Renderer>().sharedMaterial = mats["LaunchTowerRed"];
                DestroyImmediate(strut.GetComponent<Collider>());
            }

            // 2. Escape Motor Cylinder (Solid Rocket Motor, R: 0.35m, H: 4.5m)
            var motor = CreateMeshObject("LES_EscapeMotor", CreateFrustum(0.35f, 0.35f, 4.5f, 16, true, true), mats["White"], parent.transform);
            motor.transform.localPosition = new Vector3(0, baseY + 4.8f, 0);

            // 4 Canted Escape Nozzles at bottom of motor
            for (int n = 0; n < 4; n++)
            {
                float nAngle = n * 90f;
                Quaternion nRot = Quaternion.Euler(0, nAngle, 0);
                var noz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                noz.name = "EscapeNozzle_" + n;
                noz.transform.parent = parent.transform;
                noz.transform.localPosition = new Vector3(0, baseY + 5.1f, 0) + nRot * new Vector3(0.42f, 0, 0);
                noz.transform.localRotation = nRot * Quaternion.Euler(35f, 0, 0);
                noz.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
                noz.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
                DestroyImmediate(noz.GetComponent<Collider>());
            }

            // 3. Tower Jettison Motor & Aerodynamic Canards (H: 1.5m)
            var canards = CreateMeshObject("LES_CanardSection", CreateFrustum(0.35f, 0.15f, 1.5f, 16, false, false), mats["LaunchTowerRed"], parent.transform);
            canards.transform.localPosition = new Vector3(0, baseY + 9.3f, 0);

            // 4. Nose Spike with Q-ball pitot sensor (H: 1.4m)
            var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "QBall_NoseSpike";
            spike.transform.parent = parent.transform;
            spike.transform.localPosition = new Vector3(0, baseY + 11.5f, 0);
            spike.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
            spike.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
            DestroyImmediate(spike.GetComponent<Collider>());

            var qball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            qball.name = "QBall_Sensor";
            qball.transform.parent = parent.transform;
            qball.transform.localPosition = new Vector3(0, baseY + 12.2f, 0);
            qball.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            qball.GetComponent<Renderer>().sharedMaterial = mats["LaunchTowerRed"];
            DestroyImmediate(qball.GetComponent<Collider>());
        }

        // ==========================================
        // LAUNCH COMPLEX 39A (MLP Platform, LUT Tower, Lighting)
        // ==========================================
        private static GameObject BuildLaunchComplex(Dictionary<string, Material> mats)
        {
            var padRoot = new GameObject("SaturnV_LaunchComplex");
            padRoot.transform.position = Vector3.zero;

            // 1. Mobile Launcher Platform (MLP): 42m x 36m x 5m
            var mlp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mlp.name = "MobileLauncherPlatform";
            mlp.transform.parent = padRoot.transform;
            mlp.transform.localPosition = new Vector3(0, H_PAD * 0.5f, 0);
            mlp.transform.localScale = new Vector3(38f, H_PAD, 44f);
            mlp.GetComponent<Renderer>().sharedMaterial = mats["PadSteel"];

            // Flame trench exhaust well hole underneath engines (simulated with dark box)
            var exhaustWell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exhaustWell.name = "ExhaustWell";
            exhaustWell.transform.parent = padRoot.transform;
            exhaustWell.transform.localPosition = new Vector3(0, H_PAD - 0.1f, 0);
            exhaustWell.transform.localScale = new Vector3(12.5f, 0.25f, 12.5f);
            exhaustWell.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
            DestroyImmediate(exhaustWell.GetComponent<Collider>());

            // 4 Hold-Down Support Arms
            for (int h = 0; h < 4; h++)
            {
                float hAngle = h * 90f + 45f;
                Quaternion hRot = Quaternion.Euler(0, hAngle, 0);
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "HoldDownArm_" + h;
                arm.transform.parent = padRoot.transform;
                arm.transform.localPosition = new Vector3(0, H_PAD + 1.0f, 0) + hRot * new Vector3(5.6f, 0, 0);
                arm.transform.localRotation = hRot;
                arm.transform.localScale = new Vector3(1.2f, 2.0f, 1.8f);
                arm.GetComponent<Renderer>().sharedMaterial = mats["LaunchTowerRed"];
                DestroyImmediate(arm.GetComponent<Collider>());
            }

            // 2. Launch Umbilical Tower (LUT): 120m Red Steel Lattice Tower!
            var towerObj = new GameObject("LaunchUmbilicalTower_LUT");
            towerObj.transform.parent = padRoot.transform;
            towerObj.transform.localPosition = new Vector3(-13.5f, H_PAD, 0);

            float towerHeight = 118f;
            float towerWidth = 7.5f;

            // Main vertical legs of the tower
            Vector3[] colOffsets = new Vector3[]
            {
                new Vector3(-towerWidth*0.5f, 0, -towerWidth*0.5f),
                new Vector3( towerWidth*0.5f, 0, -towerWidth*0.5f),
                new Vector3(-towerWidth*0.5f, 0,  towerWidth*0.5f),
                new Vector3( towerWidth*0.5f, 0,  towerWidth*0.5f)
            };

            for (int c = 0; c < 4; c++)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "TowerLeg_" + c;
                col.transform.parent = towerObj.transform;
                col.transform.localPosition = colOffsets[c] + new Vector3(0, towerHeight * 0.5f, 0);
                col.transform.localScale = new Vector3(0.55f, towerHeight * 0.5f, 0.55f);
                col.GetComponent<Renderer>().sharedMaterial = mats["LaunchTowerRed"];
                DestroyImmediate(col.GetComponent<Collider>());
            }

            // Tower Floors & Cross Braces (every 7 meters)
            int levels = Mathf.FloorToInt(towerHeight / 7.0f);
            for (int lvl = 1; lvl <= levels; lvl++)
            {
                float lvlY = lvl * 7.0f;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "TowerFloor_" + lvl;
                floor.transform.parent = towerObj.transform;
                floor.transform.localPosition = new Vector3(0, lvlY, 0);
                floor.transform.localScale = new Vector3(towerWidth, 0.4f, towerWidth);
                floor.GetComponent<Renderer>().sharedMaterial = mats["PadSteel"];
                DestroyImmediate(floor.GetComponent<Collider>());
            }

            // 9 Swing Service Arms extending from tower to rocket!
            float[] swingArmHeights = new float[] { 8f, 18f, 32f, 44f, 56f, 68f, 80f, 94f, 106f };
            for (int sa = 0; sa < swingArmHeights.Length; sa++)
            {
                float saY = swingArmHeights[sa];
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "SwingArm_" + (sa + 1);
                arm.transform.parent = towerObj.transform;
                arm.transform.localPosition = new Vector3(5.5f, saY, 0);
                arm.transform.localScale = new Vector3(7.5f, 0.8f, 1.2f);
                arm.GetComponent<Renderer>().sharedMaterial = mats["LaunchTowerRed"];
                DestroyImmediate(arm.GetComponent<Collider>());

                // Connection umbilical pipe
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = "UmbilicalLine_" + (sa + 1);
                pipe.transform.parent = towerObj.transform;
                pipe.transform.localPosition = new Vector3(9.5f, saY, 0);
                pipe.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                pipe.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);
                pipe.GetComponent<Renderer>().sharedMaterial = mats["MetalDark"];
                DestroyImmediate(pipe.GetComponent<Collider>());
            }

            // Hammerhead Crane atop tower
            var crane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crane.name = "HammerheadCrane";
            crane.transform.parent = towerObj.transform;
            crane.transform.localPosition = new Vector3(2.0f, towerHeight + 2.5f, 0);
            crane.transform.localScale = new Vector3(14.0f, 2.2f, 2.2f);
            crane.GetComponent<Renderer>().sharedMaterial = mats["White"];
            DestroyImmediate(crane.GetComponent<Collider>());

            // Red Aviation Obstruction Beacon
            var beaconLight = new GameObject("AviationBeacon");
            beaconLight.transform.parent = towerObj.transform;
            beaconLight.transform.localPosition = new Vector3(0, towerHeight + 5f, 0);
            var bl = beaconLight.AddComponent<Light>();
            bl.type = LightType.Point;
            bl.color = Color.red;
            bl.range = 80f;
            bl.intensity = 20f;

            // 3. Ground Floodlights illuminating the Saturn V
            Vector3[] floodlightPositions = new Vector3[]
            {
                new Vector3( 26f, H_PAD + 1f,  26f),
                new Vector3(-26f, H_PAD + 1f,  26f),
                new Vector3( 26f, H_PAD + 1f, -26f),
                new Vector3(-26f, H_PAD + 1f, -26f)
            };

            for (int f = 0; f < floodlightPositions.Length; f++)
            {
                var flObj = new GameObject("PadFloodlight_" + f);
                flObj.transform.parent = padRoot.transform;
                flObj.transform.localPosition = floodlightPositions[f];

                var l = flObj.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = new Color(1.0f, 0.95f, 0.85f);
                l.range = 160f;
                l.spotAngle = 65f;
                l.intensity = 45f;
                flObj.transform.LookAt(new Vector3(0, 50f, 0));
            }

            return padRoot;
        }

        // ==========================================
        // CAMERA SETUP
        // ==========================================
        private static void SetupCameras(GameObject rocket, SaturnVFlightController flightCtrl)
        {
            // 1. Ground Tracking Camera (Dramatic low-angle view looking up at the 110m stack)
            var trackCamObj = GameObject.Find("TrackingCamera");
            if (trackCamObj == null)
            {
                trackCamObj = new GameObject("TrackingCamera");
            }
            trackCamObj.transform.position = new Vector3(45f, 12f, -55f);
            trackCamObj.transform.LookAt(new Vector3(0, 40f, 0));

            var trackCam = trackCamObj.GetComponent<Camera>();
            if (trackCam == null) trackCam = trackCamObj.AddComponent<Camera>();
            trackCam.fieldOfView = 50f;
            trackCam.nearClipPlane = 0.5f;
            trackCam.farClipPlane = 2000f;
            flightCtrl.trackingCamera = trackCam;

            // Simple camera follower
            var follower = trackCamObj.GetComponent<SmoothLookAtTarget>();
            if (follower == null) follower = trackCamObj.AddComponent<SmoothLookAtTarget>();
            follower.target = rocket.transform;
            follower.heightOffset = 30f;

            // 2. Onboard Booster Camera (attached to S-IC looking down along the rocket body)
            var onboardCamObj = GameObject.Find("OnboardBoosterCamera");
            if (onboardCamObj == null)
            {
                onboardCamObj = new GameObject("OnboardBoosterCamera");
            }
            onboardCamObj.transform.parent = rocket.transform;
            onboardCamObj.transform.localPosition = new Vector3(0, 43f, 5.8f);
            onboardCamObj.transform.localRotation = Quaternion.Euler(75f, 180f, 0);

            var onboardCam = onboardCamObj.GetComponent<Camera>();
            if (onboardCam == null) onboardCam = onboardCamObj.AddComponent<Camera>();
            onboardCam.fieldOfView = 65f;
            onboardCam.nearClipPlane = 0.3f;
            onboardCam.farClipPlane = 2000f;
            onboardCam.enabled = false;
            flightCtrl.onboardCamera = onboardCam;

            // Main camera update if exists
            var mainCam = Camera.main;
            if (mainCam != null && mainCam != trackCam && mainCam != onboardCam)
            {
                mainCam.transform.position = new Vector3(35f, 15f, -45f);
                mainCam.transform.LookAt(new Vector3(0, 35f, 0));
                mainCam.farClipPlane = 2000f;
            }
        }

        // ==========================================
        // PROCEDURAL MESH GENERATION HELPERS
        // ==========================================
        private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.parent = parent;
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            return go;
        }

        public static Mesh CreateFrustum(float bottomRadius, float topRadius, float height, int radialSegments, bool closeBottom = false, bool closeTop = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Frustum_" + bottomRadius + "_" + topRadius;

            int vertCount = (radialSegments + 1) * 2;
            if (closeBottom) vertCount += (radialSegments + 2);
            if (closeTop) vertCount += (radialSegments + 2);

            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            float slope = (bottomRadius - topRadius) / Mathf.Max(0.001f, height);
            int vIdx = 0;

            // 1. Side Vertices
            for (int i = 0; i <= radialSegments; i++)
            {
                float t = (float)i / radialSegments;
                float angle = t * Mathf.PI * 2.0f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 normal = new Vector3(cos, slope, sin).normalized;

                // Bottom vertex
                vertices[vIdx] = new Vector3(cos * bottomRadius, 0, sin * bottomRadius);
                normals[vIdx] = normal;
                uvs[vIdx] = new Vector2(t, 0f);
                vIdx++;

                // Top vertex
                vertices[vIdx] = new Vector3(cos * topRadius, height, sin * topRadius);
                normals[vIdx] = normal;
                uvs[vIdx] = new Vector2(t, 1f);
                vIdx++;
            }

            // Calculate triangle count
            int triCount = radialSegments * 6;
            if (closeBottom) triCount += radialSegments * 3;
            if (closeTop) triCount += radialSegments * 3;
            int[] triangles = new int[triCount];
            int tIdx = 0;

            // Side triangles
            for (int i = 0; i < radialSegments; i++)
            {
                int b0 = i * 2;
                int t0 = i * 2 + 1;
                int b1 = (i + 1) * 2;
                int t1 = (i + 1) * 2 + 1;

                triangles[tIdx++] = b0;
                triangles[tIdx++] = t0;
                triangles[tIdx++] = b1;

                triangles[tIdx++] = b1;
                triangles[tIdx++] = t0;
                triangles[tIdx++] = t1;
            }

            // Close Bottom Cap
            if (closeBottom)
            {
                int centerIdx = vIdx++;
                vertices[centerIdx] = Vector3.zero;
                normals[centerIdx] = Vector3.down;
                uvs[centerIdx] = new Vector2(0.5f, 0.5f);

                int rimStart = vIdx;
                for (int i = 0; i <= radialSegments; i++)
                {
                    float t = (float)i / radialSegments;
                    float angle = t * Mathf.PI * 2.0f;
                    vertices[vIdx] = new Vector3(Mathf.Cos(angle) * bottomRadius, 0, Mathf.Sin(angle) * bottomRadius);
                    normals[vIdx] = Vector3.down;
                    uvs[vIdx] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
                    vIdx++;
                }

                for (int i = 0; i < radialSegments; i++)
                {
                    triangles[tIdx++] = centerIdx;
                    triangles[tIdx++] = rimStart + i + 1;
                    triangles[tIdx++] = rimStart + i;
                }
            }

            // Close Top Cap
            if (closeTop)
            {
                int centerIdx = vIdx++;
                vertices[centerIdx] = new Vector3(0, height, 0);
                normals[centerIdx] = Vector3.up;
                uvs[centerIdx] = new Vector2(0.5f, 0.5f);

                int rimStart = vIdx;
                for (int i = 0; i <= radialSegments; i++)
                {
                    float t = (float)i / radialSegments;
                    float angle = t * Mathf.PI * 2.0f;
                    vertices[vIdx] = new Vector3(Mathf.Cos(angle) * topRadius, height, Mathf.Sin(angle) * topRadius);
                    normals[vIdx] = Vector3.up;
                    uvs[vIdx] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
                    vIdx++;
                }

                for (int i = 0; i < radialSegments; i++)
                {
                    triangles[tIdx++] = centerIdx;
                    triangles[tIdx++] = rimStart + i;
                    triangles[tIdx++] = rimStart + i + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateFinMesh(float rootChord, float tipChord, float span, float thickness, float sweepBack)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SaturnV_Fin";

            float t2 = thickness * 0.5f;

            // 8 Box-like vertices representing the swept trapezoidal fin
            Vector3[] v = new Vector3[]
            {
                // Root inner edge (x = 0)
                new Vector3(0, 0, -t2),
                new Vector3(0, 0,  t2),
                new Vector3(0, rootChord, -t2),
                new Vector3(0, rootChord,  t2),

                // Tip outer edge (x = span)
                new Vector3(span, -sweepBack, -t2 * 0.5f),
                new Vector3(span, -sweepBack,  t2 * 0.5f),
                new Vector3(span, tipChord - sweepBack, -t2 * 0.5f),
                new Vector3(span, tipChord - sweepBack,  t2 * 0.5f)
            };

            int[] tri = new int[]
            {
                // Sides (+Z and -Z)
                1, 5, 3,  3, 5, 7,
                0, 2, 4,  2, 6, 4,
                // Leading edge (Top)
                2, 3, 6,  3, 7, 6,
                // Trailing edge (Bottom)
                0, 4, 1,  1, 4, 5,
                // Outer tip edge
                4, 6, 5,  5, 6, 7
            };

            mesh.vertices = v;
            mesh.triangles = tri;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ==========================================
        // MATERIAL CREATION & PROCEDURAL TEXTURES
        // ==========================================
        private static Dictionary<string, Material> CreateRocketMaterials()
        {
            var dict = new Dictionary<string, Material>();
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            // 1. Off-white Saturn V Apollo White
            var matWhite = new Material(urpShader);
            matWhite.name = "M_SaturnV_White";
            matWhite.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f));
            matWhite.SetFloat("_Smoothness", 0.55f);
            matWhite.SetFloat("_Metallic", 0.1f);
            dict["White"] = matWhite;

            // 2. Corrugated Dark / Black Band
            var matBlack = new Material(urpShader);
            matBlack.name = "M_SaturnV_Black";
            matBlack.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.13f));
            matBlack.SetFloat("_Smoothness", 0.4f);
            matBlack.SetFloat("_Metallic", 0.2f);
            dict["BlackBand"] = matBlack;

            // 3. F-1 Engine Bell Gunmetal
            var matEngine = new Material(urpShader);
            matEngine.name = "M_Engine_F1";
            matEngine.SetColor("_BaseColor", new Color(0.22f, 0.23f, 0.25f));
            matEngine.SetFloat("_Smoothness", 0.45f);
            matEngine.SetFloat("_Metallic", 0.85f);
            dict["EngineF1"] = matEngine;

            // 4. Metal Dark / Heatshield
            var matDark = new Material(urpShader);
            matDark.name = "M_Metal_Dark";
            matDark.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.16f));
            matDark.SetFloat("_Smoothness", 0.6f);
            matDark.SetFloat("_Metallic", 0.75f);
            dict["MetalDark"] = matDark;

            // 5. Metal Silver / Aluminum (Service Module)
            var matSilver = new Material(urpShader);
            matSilver.name = "M_Metal_Silver";
            matSilver.SetColor("_BaseColor", new Color(0.85f, 0.87f, 0.90f));
            matSilver.SetFloat("_Smoothness", 0.72f);
            matSilver.SetFloat("_Metallic", 0.85f);
            dict["MetalSilver"] = matSilver;

            // 6. Apollo Command Module Capsule
            var matCM = new Material(urpShader);
            matCM.name = "M_CommandModule";
            matCM.SetColor("_BaseColor", new Color(0.70f, 0.70f, 0.72f));
            matCM.SetFloat("_Smoothness", 0.8f);
            matCM.SetFloat("_Metallic", 0.9f);
            dict["CommandModule"] = matCM;

            // 7. Gold Foil (Lunar Module)
            var matGold = new Material(urpShader);
            matGold.name = "M_GoldFoil";
            matGold.SetColor("_BaseColor", new Color(0.92f, 0.72f, 0.18f));
            matGold.SetFloat("_Smoothness", 0.85f);
            matGold.SetFloat("_Metallic", 0.95f);
            dict["GoldFoil"] = matGold;

            // 8. Launch Umbilical Tower Red (NASA red-orange)
            var matTower = new Material(urpShader);
            matTower.name = "M_LaunchTowerRed";
            matTower.SetColor("_BaseColor", new Color(0.82f, 0.18f, 0.10f));
            matTower.SetFloat("_Smoothness", 0.4f);
            matTower.SetFloat("_Metallic", 0.3f);
            dict["LaunchTowerRed"] = matTower;

            // 9. Launch Platform Steel Gray
            var matPad = new Material(urpShader);
            matPad.name = "M_PadSteel";
            matPad.SetColor("_BaseColor", new Color(0.28f, 0.30f, 0.32f));
            matPad.SetFloat("_Smoothness", 0.35f);
            matPad.SetFloat("_Metallic", 0.6f);
            dict["PadSteel"] = matPad;

            // 10. Procedural Textures for S-IC Roll Pattern & S-II/S-IVB Markings
            var texSIC = GenerateSICRollPatternTexture();
            var matSIC = new Material(urpShader);
            matSIC.name = "M_SIC_RollPattern";
            matSIC.SetTexture("_BaseMap", texSIC);
            matSIC.SetFloat("_Smoothness", 0.5f);
            dict["SIC_RollPattern"] = matSIC;

            var texSII = GenerateStageMarkingTexture("UNITED STATES", true);
            var matSII = new Material(urpShader);
            matSII.name = "M_SII_Markings";
            matSII.SetTexture("_BaseMap", texSII);
            matSII.SetFloat("_Smoothness", 0.5f);
            dict["SII_Markings"] = matSII;

            var texSIVB = GenerateStageMarkingTexture("USA", false);
            var matSIVB = new Material(urpShader);
            matSIVB.name = "M_SIVB_Markings";
            matSIVB.SetTexture("_BaseMap", texSIVB);
            matSIVB.SetFloat("_Smoothness", 0.5f);
            dict["SIVB_Markings"] = matSIVB;

            // 11. Flame Particle Material
            var flameShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (flameShader == null) flameShader = Shader.Find("Particles/Standard Unlit");
            var matFlame = new Material(flameShader != null ? flameShader : urpShader);
            matFlame.name = "M_RocketFlame";
            matFlame.SetColor("_BaseColor", new Color(1.0f, 0.6f, 0.15f, 1.0f));
            dict["Flame"] = matFlame;

            return dict;
        }

        // Generates the iconic Saturn V 4-quadrant black & white roll pattern with crisp checker squares
        private static Texture2D GenerateSICRollPatternTexture()
        {
            int w = 512;
            int h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);

            Color whiteCol = new Color(0.95f, 0.95f, 0.95f);
            Color blackCol = new Color(0.12f, 0.12f, 0.13f);
            Color redCol = new Color(0.85f, 0.15f, 0.15f);

            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;

                    // 4 quadrants around circumference: Q0: 0-0.25, Q1: 0.25-0.5, Q2: 0.5-0.75, Q3: 0.75-1.0
                    bool isBlackQuadrant = (u >= 0.0f && u < 0.25f) || (u >= 0.5f && u < 0.75f);

                    // Vertical roll checker bands on Saturn V
                    // Top 15% and bottom 10% have checker reversals
                    if (v > 0.82f)
                    {
                        // Inverted checker header
                        isBlackQuadrant = !isBlackQuadrant;
                    }

                    Color c = isBlackQuadrant ? blackCol : whiteCol;

                    // Red 'UNITED STATES' band accent near upper section
                    if (v >= 0.74f && v <= 0.76f)
                    {
                        c = redCol;
                    }

                    // Panel seam lines
                    if (y % 64 == 0 || x % 128 == 0)
                    {
                        c = Color.Lerp(c, Color.black, 0.25f);
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateStageMarkingTexture(string label, bool includeBand)
        {
            int w = 512;
            int h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);

            Color whiteCol = new Color(0.95f, 0.95f, 0.95f);
            Color blackCol = new Color(0.12f, 0.12f, 0.13f);
            Color redCol = new Color(0.85f, 0.15f, 0.15f);

            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;

                    Color c = whiteCol;

                    // Black vertical stripe on quadrant 1
                    if (u >= 0.22f && u <= 0.28f)
                    {
                        c = blackCol;
                    }

                    // Circumferential black interstage line
                    if (includeBand && (v <= 0.08f || v >= 0.92f))
                    {
                        c = blackCol;
                    }

                    // Red logo stripe
                    if (u >= 0.65f && u <= 0.85f && v >= 0.45f && v <= 0.55f)
                    {
                        c = redCol;
                    }

                    // Seam rivets
                    if (y % 32 == 0)
                    {
                        c = Color.Lerp(c, Color.black, 0.15f);
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void ConfigureEngineParticleSystem(ParticleSystem ps, ParticleSystemRenderer psr, Material flameMat)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 35f;
            main.startSize = 2.4f;
            main.startColor = new Color(1.0f, 0.65f, 0.2f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 250f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 1.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.4f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.5f);
            curve.AddKey(0.3f, 1.2f);
            curve.AddKey(1.0f, 2.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            if (psr != null && flameMat != null)
            {
                psr.sharedMaterial = flameMat;
            }
        }
    }

    // Camera tracker helper
    public class SmoothLookAtTarget : MonoBehaviour
    {
        public Transform target;
        public float heightOffset = 35f;
        public float smoothSpeed = 3f;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 lookPos = target.position + Vector3.up * heightOffset;
            Quaternion desiredRot = Quaternion.LookRotation(lookPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * smoothSpeed);
        }
    }
}
