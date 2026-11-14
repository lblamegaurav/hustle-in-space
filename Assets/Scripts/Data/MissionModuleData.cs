using System;
using System.Collections.Generic;

namespace SpaceSim.Data
{
    [Serializable]
    public class RocketModuleInfo
    {
        public string id;
        public string displayName;
        public string subtitle;
        public string manufacturer;
        public string dimensions;
        public string massSpecs;
        public string propulsionChemistry;
        public string thrustAndIsp;
        public string structuralMaterials;
        public string technicalHazards;
        public string historicalSignificance;
        public string gameObjectName; // Name of GameObject in 3D scene to focus/highlight
        public float cameraFocusY;    // Vertical offset for framing in 3D viewport
        public float cameraDistance;  // Optimal view distance
    }

    [Serializable]
    public class MissionDefinition
    {
        public string missionId;
        public string missionCode;
        public string missionTitle;
        public string historicalDate;
        public string crewManifest;
        public string primaryObjective;
        public string vehicleName;
        public string overallSpecs;
        public List<RocketModuleInfo> modules = new List<RocketModuleInfo>();
    }

    public static class MissionDatabase
    {
        private static List<MissionDefinition> _missions;

        public static List<MissionDefinition> GetAllMissions()
        {
            if (_missions == null) Initialize();
            return _missions;
        }

        public static MissionDefinition GetMission(string missionId)
        {
            if (_missions == null) Initialize();
            return _missions.Find(m => m.missionId.Equals(missionId, StringComparison.OrdinalIgnoreCase));
        }

        private static void Initialize()
        {
            _missions = new List<MissionDefinition>();

            // ==========================================
            // MISSION 1: APOLLO 11 / SATURN V
            // ==========================================
            var saturnV = new MissionDefinition
            {
                missionId = "SaturnV",
                missionCode = "APOLLO 11",
                missionTitle = "Apollo 11 — Saturn V Lunar Expedition",
                historicalDate = "July 16 – 24, 1969",
                crewManifest = "Neil Armstrong (CDR), Michael Collins (CMP), Edwin 'Buzz' Aldrin (LMP)",
                primaryObjective = "Execute human lunar landing in the Sea of Tranquility and safe return to Earth.",
                vehicleName = "Saturn V (SA-506) / CSM-107 'Columbia' / LM-5 'Eagle'",
                overallSpecs = "Height: 110.6 m (363 ft) | Launch Mass: 2,965,000 kg | Liftoff Thrust: 35.1 MN (7.8M lbf) | Payload to LEO: 140,000 kg | Payload to TLI: 48,600 kg",
                modules = new List<RocketModuleInfo>
                {
                    new RocketModuleInfo
                    {
                        id = "saturn_sic",
                        displayName = "S-IC First Stage",
                        subtitle = "Boeing Heavy-Lift Booster (5x Rocketdyne F-1 Engines)",
                        manufacturer = "The Boeing Company (Michoud Assembly Facility, New Orleans)",
                        dimensions = "Height: 42.0 m (138 ft) | Diameter: 10.1 m (33 ft)",
                        massSpecs = "Gross Mass: ~2,280,000 kg (5.03M lbs) | Dry Mass: ~131,000 kg (288,000 lbs)",
                        propulsionChemistry = "RP-1 (Rocket Propellant-1 Kerosene) & LOX (Liquid Oxygen) at 2.27:1 mass oxidizer-to-fuel ratio",
                        thrustAndIsp = "Sea-Level Thrust: 35.1 MN (7,891,000 lbf) total (7.0 MN per F-1) | Sea-Level Isp: 263 s | Vacuum Isp: 304 s | Burn Time: 168 seconds to 68 km altitude (Mach 7.8)",
                        structuralMaterials = "Aluminum Alloy 2219-T87 high-strength welded tanks; ring-stiffened waffled skin panels resisting 35 MN compressive launch load",
                        technicalHazards = "Severe combustion instability in massive F-1 thrust chambers solved by copper acoustic baffles; intense longitudinal Pogo oscillation during high-g ascent",
                        historicalSignificance = "Most powerful single-combustion-chamber booster ever successfully flown; 100% mission flight success rate across 13 Apollo & Skylab launches",
                        gameObjectName = "Stage1_S-IC",
                        cameraFocusY = 25f,
                        cameraDistance = 55f
                    },
                    new RocketModuleInfo
                    {
                        id = "saturn_sii",
                        displayName = "S-II Second Stage",
                        subtitle = "North American Aviation Cryogenic Booster (5x Rocketdyne J-2 Engines)",
                        manufacturer = "North American Aviation (Seal Beach, California)",
                        dimensions = "Height: 24.8 m (81.5 ft) | Diameter: 10.1 m (33 ft)",
                        massSpecs = "Gross Mass: ~480,000 kg (1.06M lbs) | Dry Mass: ~36,000 kg (79,000 lbs)",
                        propulsionChemistry = "Liquid Hydrogen (LH2 at 20 K) & Liquid Oxygen (LOX at 90 K) at 5.5:1 ratio",
                        thrustAndIsp = "Vacuum Thrust: 5.14 MN (1,155,000 lbf) total (1.03 MN per J-2) | Vacuum Isp: 421 s (4.13 km/s exhaust velocity) | Burn Time: ~360 seconds to 175 km altitude",
                        structuralMaterials = "Pioneered lightweight Common Bulkhead: 2014-T6 aluminum skins separated by a phenolic fiberglass honeycomb core (25 mm thick), insulating 70°C differential and saving 4.1 metric tons",
                        technicalHazards = "Cryogenic boil-off of liquid hydrogen; center J-2 engine vulnerable to pogo resonance (premature center engine shutdown on Apollo 13 was autonomously compensated by IU)",
                        historicalSignificance = "Mastered high-energy cryogenic liquid hydrogen at an unprecedented aerospace scale, enabling insertion into near-orbital velocity",
                        gameObjectName = "Stage2_S-II",
                        cameraFocusY = 58f,
                        cameraDistance = 45f
                    },
                    new RocketModuleInfo
                    {
                        id = "saturn_sivb",
                        displayName = "S-IVB Third Stage",
                        subtitle = "Douglas Aircraft Translunar Injection Stage (1x Restartable J-2)",
                        manufacturer = "Douglas Aircraft Company (Huntington Beach, California)",
                        dimensions = "Height: 18.0 m (59 ft) | Diameter: 6.6 m (21.7 ft)",
                        massSpecs = "Gross Mass: ~120,000 kg (265,000 lbs) | Dry Mass: ~11,000 kg (24,000 lbs)",
                        propulsionChemistry = "Cryogenic Liquid Hydrogen (LH2) & Liquid Oxygen (LOX)",
                        thrustAndIsp = "Vacuum Thrust: 1.03 MN (232,000 lbf) | Vacuum Isp: 421 s | Burn 1: 2.5 min (Earth parking orbit 185 km) | Burn 2: 5.8 min (Translunar Injection burn to 40,000 km/h / 25,000 mph)",
                        structuralMaterials = "Internally isogrid-machined aluminum tankage; external polyurethane foam insulation; two Auxiliary Propulsion System (APS) attitude modules",
                        technicalHazards = "In-space restart in zero-g requiring solid ullage rockets to settle cryogenic propellants and helium turbopump pre-chilling to prevent vapor cavitation",
                        historicalSignificance = "Fired twice in space: first circularized Apollo in Earth orbit, then ignited over the Pacific to blast the crew out of Earth's gravity well toward the Moon",
                        gameObjectName = "Stage3_S-IVB",
                        cameraFocusY = 78f,
                        cameraDistance = 40f
                    },
                    new RocketModuleInfo
                    {
                        id = "saturn_iu",
                        displayName = "Saturn Instrument Unit (IU)",
                        subtitle = "IBM Autonomous Digital Guidance & Control Computer Ring",
                        manufacturer = "IBM Federal Systems Division (Huntsville, Alabama)",
                        dimensions = "Height: 0.9 m (3 ft) | Diameter: 6.6 m (21.7 ft)",
                        massSpecs = "Total Mass: ~2,000 kg (4,400 lbs)",
                        propulsionChemistry = "Non-propulsive; 28V DC zinc-silver oxide batteries; methanol-water thermal conditioning system",
                        thrustAndIsp = "Computer Clock: 2.048 MHz | Triple-Modular Redundancy (TMR) voting logic on critical arithmetic units; ST-124-M3 inertial platform gyroscopes",
                        structuralMaterials = "Three-segment extruded aluminum honeycomb sandwich cylinder; thermally jacketed cold plates",
                        technicalHazards = "Atmospheric wind shear compensation; saved Apollo 12 after two lightning strikes scrambled cabin telemetry by maintaining uncorrupted booster guidance",
                        historicalSignificance = "The brain of the Saturn V: autonomously steered all three rocket stages from pad liftoff through Translunar Injection without astronaut manual steering required",
                        gameObjectName = "InstrumentUnit_IU",
                        cameraFocusY = 88f,
                        cameraDistance = 30f
                    },
                    new RocketModuleInfo
                    {
                        id = "saturn_lm_sla",
                        displayName = "Spacecraft-LM Adapter & Lunar Module",
                        subtitle = "Grumman LM-5 'Eagle' housed inside SLA Conical Fairing",
                        manufacturer = "Grumman Aerospace Corporation (LM) / North American Aviation (SLA)",
                        dimensions = "SLA Height: 8.5 m (28 ft) | Base Dia: 6.6 m | Top Dia: 3.9 m | LM Height: 7.0 m",
                        massSpecs = "SLA Mass: ~1,800 kg | Lunar Module Total Mass: ~15,200 kg (Descent: 10,300 kg, Ascent: 4,900 kg)",
                        propulsionChemistry = "Aerozine 50 & Nitrogen Tetroxide (N2O4) hypergolic storable propellants (spontaneous ignition upon contact)",
                        thrustAndIsp = "Descent Engine: 45 kN (10,500 lbf) deep-throttling (10% to 100%) | Ascent Engine: 16 kN (3,500 lbf) constant thrust",
                        structuralMaterials = "Mylar, Kapton aluminized gold-foil thermal blankets; ultra-lightweight 0.012-inch chemically-milled aluminum alloy skin panels",
                        technicalHazards = "Zero margin for landing radar anomalies; manual pilot landing by Armstrong with <30 seconds of descent propellant remaining over boulder crater",
                        historicalSignificance = "First dedicated crewed spacecraft designed strictly to operate in the vacuum of space; safely landed Neil Armstrong and Buzz Aldrin on the lunar surface on July 20, 1969",
                        gameObjectName = "Spacecraft_LM_Adapter",
                        cameraFocusY = 94f,
                        cameraDistance = 32f
                    },
                    new RocketModuleInfo
                    {
                        id = "saturn_csm_les",
                        displayName = "Apollo CSM & Launch Escape Tower",
                        subtitle = "Command & Service Module 'Columbia' + Solid Escape Rocket",
                        manufacturer = "North American Aviation (Downey, California)",
                        dimensions = "CSM Height: 11.0 m (36 ft) | LES Height: 10.2 m (33 ft) | Max Dia: 3.9 m (12.8 ft)",
                        massSpecs = "Total CSM Mass: ~28,800 kg (63,500 lbs) | CM: 5,800 kg | SM: 23,000 kg | LES Mass: ~4,200 kg",
                        propulsionChemistry = "SPS (Service Propulsion System): Aerozine 50 / N2O4 | LES: Solid composite propellant motor (654 kN thrust in milliseconds)",
                        thrustAndIsp = "SPS Thrust: 91 kN (20,500 lbf) | LES Thrust: 654 kN (147,000 lbf) capable of pulling Command Module away at 15 g acceleration during pad abort",
                        structuralMaterials = "Command module stainless steel honeycomb between aluminum facesheets; ablative epoxy resin heat shield (surviving 2,800°C reentry at 40,000 km/h)",
                        technicalHazards = "Steep reentry corridor: too shallow and capsule skips off atmosphere into deep space; too steep and crew undergoes lethal deceleration forces",
                        historicalSignificance = "Carried the Apollo crew across 240,000 miles of cislunar space; Michael Collins remained in lunar orbit aboard Columbia while Eagle touched down below",
                        gameObjectName = "Apollo_CSM",
                        cameraFocusY = 105f,
                        cameraDistance = 35f
                    }
                }
            };

            // ==========================================
            // MISSION 2: STS-107 / SPACE SHUTTLE COLUMBIA
            // ==========================================
            var spaceShuttle = new MissionDefinition
            {
                missionId = "SpaceShuttle",
                missionCode = "STS-107",
                missionTitle = "STS-107 — Space Shuttle Columbia (Kalpana Chawla Memorial)",
                historicalDate = "January 16 – February 1, 2003",
                crewManifest = "Rick Husband (CDR), William McCool (PLT), David Brown (MS), Kalpana Chawla (MS), Michael Anderson (Payload CDR), Laurel Clark (MS), Ilan Ramon (PS)",
                primaryObjective = "Conduct 80+ multidisciplinary microgravity experiments in SPACEHAB module; orbital science & re-entry operations.",
                vehicleName = "Space Shuttle Columbia (OV-102) / External Tank (ET-93) / Twin SRBs",
                overallSpecs = "Total Stack Height: 56.1 m (184 ft) | Liftoff Mass: 2,050,000 kg (4.5M lbs) | Liftoff Thrust: 30.16 MN (6.8M lbf) | Payload to LEO: 24,400 kg | Reusable Winged Spaceplane",
                modules = new List<RocketModuleInfo>
                {
                    new RocketModuleInfo
                    {
                        id = "shuttle_orbiter",
                        displayName = "Orbiter Vehicle (OV-102 Columbia)",
                        subtitle = "Reusable Winged Spaceplane & Pressurized Crew Cabin",
                        manufacturer = "Rockwell International (Palmdale, California)",
                        dimensions = "Length: 37.24 m (122.2 ft) | Wingspan: 23.79 m (78.1 ft) | Height: 17.86 m (58.6 ft)",
                        massSpecs = "Empty Mass: ~68,500 kg (151,000 lbs) | Landing Mass: ~84,000 kg (185,000 lbs)",
                        propulsionChemistry = "RCS & OMS: Monomethylhydrazine (MMH) & Nitrogen Tetroxide (N2O4) hypergolic propellants",
                        thrustAndIsp = "OMS Thrust: 2x 26.7 kN (6,000 lbf each) | Vacuum Isp: 316 s | Max Reentry Velocity: Mach 25 (28,000 km/h)",
                        structuralMaterials = "Aircraft aluminum alloy (2024-T3) skin with titanium internal thrust structure; double-delta planform wings for 1,800 km hypersonic cross-range glide",
                        technicalHazards = "Parallel side-by-side launch mounting made orbiter skin uniquely vulnerable to falling external tank foam shedding; dead-stick unpowered glider landing on runway with zero go-around capability",
                        historicalSignificance = "First operational reusable spacecraft in human history; Columbia flew 28 space missions between 1981 and 2003, pioneering orbital science and satellite repair",
                        gameObjectName = "Orbiter_Root",
                        cameraFocusY = 18f,
                        cameraDistance = 38f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_srb",
                        displayName = "Solid Rocket Boosters (Twin SRBs)",
                        subtitle = "Thiokol Reusable APCP Solid Motors (Largest Ever Flown With Crew)",
                        manufacturer = "Thiokol Chemical Corporation (Wasatch, Utah)",
                        dimensions = "Height: 45.46 m (149.2 ft) | Diameter: 3.71 m (12.2 ft)",
                        massSpecs = "Total Pair Liftoff Mass: ~1,180,000 kg (2.6M lbs) | Empty Mass per Booster: ~87,500 kg",
                        propulsionChemistry = "Solid Propellant Grain: 69.6% Ammonium Perchlorate (oxidizer), 16% atomized Aluminum powder (fuel), 0.4% Iron Oxide, 12% PBAN polymer binder",
                        thrustAndIsp = "Sea-Level Thrust: 13.8 MN (3.1M lbf) each; 27.6 MN (6.2M lbf) combined (71-83% of total liftoff thrust!) | Sea-Level Isp: 242 s | Burn Time: 124 seconds to 45 km altitude",
                        structuralMaterials = "D6AC high-strength steel casing segments joined by clevis-and-tang field joints sealed by double rubber O-rings and fluorocarbon vulcanized bands",
                        technicalHazards = "Solid rocket motors cannot be shut down or throttled once ignited off the pad; vulnerability of O-ring elasticity in cold launch ambient temperatures (STS-51L Challenger)",
                        historicalSignificance = "Separated at T+2m04s via pyrotechnic bolts and jettison rockets; parachuted into the Atlantic Ocean, recovered by retrieval ships MV Freedom Star and reused up to 10+ times",
                        gameObjectName = "SRB_Pair",
                        cameraFocusY = 22f,
                        cameraDistance = 42f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_et",
                        displayName = "Super Lightweight External Tank (SLWT)",
                        subtitle = "Lockheed Martin Cryogenic LH2/LOX Backbone Tank",
                        manufacturer = "Lockheed Martin (Michoud Assembly Facility, New Orleans)",
                        dimensions = "Length: 46.9 m (154 ft) | Diameter: 8.4 m (27.6 ft)",
                        massSpecs = "Gross Mass: ~760,000 kg (1.68M lbs) | Dry Mass: ~26,500 kg (58,500 lbs)",
                        propulsionChemistry = "Liquid Hydrogen (LH2: 104,300 kg at 20 K) in forward tank & Liquid Oxygen (LOX: 629,300 kg at 90 K) in aft tank",
                        thrustAndIsp = "Non-propulsive; feeds 3x RS-25 main engines through 43 cm (17 in) umbilical disconnect valves at 1,820 L of LOX and 6,100 L of LH2 per second",
                        structuralMaterials = "Aluminum-Lithium alloy (Al-Li 2195) welded grid structure covered by spray-on polyisocyanurate foam (NCFI) insulation for cryogenic temperature maintenance",
                        technicalHazards = "Foam shedding: On STS-107, an 800 g piece of bipod foam detached at T+81.9s, striking Columbia's left wing RCC panel at ~800 km/h and creating a fatal breach",
                        historicalSignificance = "The structural spine holding the SRBs and Orbiter together; the only non-reusable element of the shuttle stack, deliberately disposed into the Indian/Pacific ocean after MECO",
                        gameObjectName = "ExternalTank_Root",
                        cameraFocusY = 24f,
                        cameraDistance = 45f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_ssme",
                        displayName = "RS-25 Main Engines (SSME x3)",
                        subtitle = "Aerojet Rocketdyne High-Pressure Staged-Combustion Cryogenic Engines",
                        manufacturer = "Rocketdyne / Aerojet Rocketdyne (Canoga Park, California)",
                        dimensions = "Length: 4.27 m (14 ft) | Nozzle Diameter: 2.4 m (7.9 ft)",
                        massSpecs = "Mass per Engine: ~3,527 kg (7,775 lbs) | Total Triple Cluster Mass: ~10,580 kg",
                        propulsionChemistry = "Liquid Hydrogen (LH2) & Liquid Oxygen (LOX) staged-combustion cycle",
                        thrustAndIsp = "Sea-Level Thrust: 1.75 MN (393,000 lbf) per engine | Vacuum Thrust: 2.28 MN (512,000 lbf) at 104% throttle | Vacuum Isp: 452.3 s (4.44 km/s exhaust velocity!) | Chamber Pressure: 206 bar (3,000 psi)",
                        structuralMaterials = "Inconel-718 superalloys; regeneratively cooled copper alloy nozzle liner carrying super-cold LH2 through 1,080 brazed coolant tubes",
                        technicalHazards = "Turbopumps spinning at 35,000 RPM at 480 bar fluid pressure with temperature gradients spanning -250°C to +3,300°C over just a few centimeters of metal",
                        historicalSignificance = "Highest efficiency rocket engines in aerospace history; each RS-25 was designed for 55 re-flights, and updated flight-proven engines now power NASA's SLS Artemis moon rockets",
                        gameObjectName = "RS25_Engines",
                        cameraFocusY = 8f,
                        cameraDistance = 26f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_tps",
                        displayName = "Thermal Protection System (TPS)",
                        subtitle = "24,000 HRSI Silica Tiles & Reinforced Carbon-Carbon (RCC) Panels",
                        manufacturer = "Lockheed Missiles & Space / BFGoodrich Aerospace",
                        dimensions = "Covers entire orbiter exterior (~1,100 m² of exposed surface area)",
                        massSpecs = "Total TPS Mass: ~7,250 kg (16,000 lbs)",
                        propulsionChemistry = "Passive aerothermal radiative protection against hypersonic shock plasma (>1,650°C)",
                        thrustAndIsp = "HRSI tiles radiate 90% of atmospheric friction heat while keeping inner aluminum airframe below 175°C; 99.8% pure amorphous silica fibers (90% air by volume)",
                        structuralMaterials = "Black HRSI silica tiles on underbelly; RCC (Reinforced Carbon-Carbon) on nose cap and wing leading edges; white LRSI & Nomex quilted blankets on upper fuselage",
                        technicalHazards = "Tiles are as fragile as chalkboard chalk; zero structural redundancy: a single hole in an RCC panel allows superheated plasma to enter the interior wing structure",
                        historicalSignificance = "Allowed a spacecraft to transition from orbital speeds of Mach 25 down to subsonic runway touchdown without burning up; central focus of the STS-107 investigation",
                        gameObjectName = "TPS_HeatShield",
                        cameraFocusY = 16f,
                        cameraDistance = 35f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_payload",
                        displayName = "Payload Bay, Canadarm & SPACEHAB",
                        subtitle = "18.3m Unpressurized Cargo Bay, Robotic Arm & Science Lab",
                        manufacturer = "Canadian Space Agency / Spar Aerospace (RMS) & Astrotech (SPACEHAB)",
                        dimensions = "Bay Length: 18.3 m (60 ft) | Diameter: 4.6 m (15 ft) | Arm Reach: 15.2 m (50 ft)",
                        massSpecs = "SPACEHAB Research Double Module: ~4,360 kg | Total Payload Capacity: 24,400 kg (53,800 lbs)",
                        propulsionChemistry = "Freon-21 fluid radiators mounted on the inside of the opening payload bay doors for vehicle heat rejection",
                        thrustAndIsp = "Canadarm 6-Degree-of-Freedom manipulator; electric servo motors capable of berthing 30-ton payloads with millimeter precision",
                        structuralMaterials = "Carbon fiber reinforced plastic (CFRP) tubular boom structure; gold-coated thermal blankets",
                        technicalHazards = "Payload bay doors must open in orbit to expose heat radiators; if doors jammed closed, mission had to abort within hours due to avionics overheating",
                        historicalSignificance = "Turned the shuttle into an orbiting national laboratory; on STS-107, Dr. Kalpana Chawla oversaw 80+ breakthrough biological, crystal growth, and physical sciences experiments in the SPACEHAB",
                        gameObjectName = "PayloadBay_Root",
                        cameraFocusY = 19f,
                        cameraDistance = 34f
                    },
                    new RocketModuleInfo
                    {
                        id = "shuttle_cockpit",
                        displayName = "Flight Deck & Kalpana Chawla Station",
                        subtitle = "Glass Cockpit Avionics & Mission Specialist Flight Console",
                        manufacturer = "Honeywell / IBM / Rockwell Collins",
                        dimensions = "Two-level pressurized cabin (upper flight deck and lower mid-deck living quarters)",
                        massSpecs = "Cabin Volume: 74.3 m³ (2,625 cu ft) | Designed for up to 8 astronauts",
                        propulsionChemistry = "Cabin Atmosphere: 21% O2 / 78% N2 at sea-level pressure (14.7 psi / 101.3 kPa)",
                        thrustAndIsp = "Avionics: 5x redundant IBM AP-101 General Purpose Computers (GPC) running voting algorithms for fly-by-wire flight guidance; 11 multi-function color display units (MEDS)",
                        structuralMaterials = "Welded 2219 aluminum pressure hull; multi-pane fused silica windshield panels capable of withstanding micrometeoroids and reentry friction",
                        technicalHazards = "High-g ascent and dead-stick landing requiring split-second decisions; master caution & warning alarms monitoring 1,000+ telemetry sensors",
                        historicalSignificance = "Dr. Kalpana Chawla served as Mission Specialist-2 / Flight Engineer, monitoring ascent systems, operating the SPACEHAB science console, and inspiring millions of aspiring scientists worldwide",
                        gameObjectName = "Cockpit_Avionics",
                        cameraFocusY = 22f,
                        cameraDistance = 24f
                    }
                }
            };

            _missions.Add(saturnV);
            _missions.Add(spaceShuttle);
        }
    }
}
