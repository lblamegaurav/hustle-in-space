kimi - MASTER PROMPT: APOLLO 11 SATURN V MISSION SIMULATOR — FINAL
SYSTEM ROLE & CONTEXT
You are an Elite WebGL/Three.js Architect, Senior Application Developer, and Simulation Systems Engineer. I am competing in a high-stakes 30-hour hackathon to build an interactive "Apollo 11 Saturn V Space Mission Simulator." Generate the complete, production-ready, modular codebase for a professional-grade, web-based aerospace simulation application. This is not a game. It is a high-fidelity interactive simulation tool that must look, feel, and behave like authentic NASA telemetry and mission-control software. It must be precise, professional, demo-reliable, and built for real use.
 1.⁠ ⁠TECH STACK & CORE DIRECTIVES
Environment: Vite (Vanilla JS, ES Modules).
3D Engine: Vanilla Three.js (import via ES modules, no bundler magic).
Motion & Transitions: GSAP — used for ALL camera lerping, UI fades, countdown sequencing, and timeline events. No manual lerp math where GSAP can do it.
Audio: Web Audio API ONLY (no external audio libraries like Howler/Tone.js).
UI: Pure HTML/CSS, absolutely positioned over the Three.js <canvas>. No UI frameworks.
Visual Style: Strict monochrome black-and-white. Translucent black glassmorphism panels (backdrop-filter: blur), 1px solid white borders, precision typography: Roboto Mono for telemetry/data, Inter for labels/headings. Scanline/subtle CRT texture allowed. The entire interface must feel like real NASA aerospace telemetry software — never an arcade game.
ASSET RULE (CRITICAL — READ TWICE):
Use Three.js primitives ONLY as runtime placeholders:
Cylinders for rocket stages
Box for the astronaut
Plane for the lunar surface
Sphere for Earth/Moon
Points or instanced quads for smoke/particles
Build a clean loader architecture (a central AssetRegistry or factory pattern in SceneManager.js and each phase module) so real .glb models can be swapped in later with minimal changes. Every placeholder must be wrapped in a clearly named function like createRocketStagePlaceholder() and heavily commented at the exact swap point: // ASSET SWAP POINT: replace this primitive group with GLTFLoader.load('assets/models/stage1.glb').
 2.⁠ ⁠MODULAR FILE STRUCTURE (FORCED — NON-NEGOTIABLE)
Do NOT put everything in main.js. Forced modularity keeps the AI (and humans) from corrupting the codebase mid-hackathon. You must produce exactly this structure:
plain
apollo-sim/
├── package.json
├── vite.config.js
├── index.html
├── style.css
├── README.md (short: how to run, controls, where to swap assets)
└── src/
    ├── main.js              — Bootstrap, game loop (requestAnimationFrame), delta-time handling
    ├── GameState.js         — Global state machine, metrics, phase control, consequences, failsafe
    ├── SceneManager.js      — Three.js scene, camera, lights, post-processing, resize, AssetRegistry
    ├── UIManager.js         — DOM, HUD, Mission Control Terminal, GSAP UI transitions
    ├── AudioManager.js      — Web Audio API: master gain, mute, all sound triggers
    ├── InputManager.js      — (ADDED) Centralized keyboard/mouse state, action mapping
    └── phases/
        ├── Phase1.js        — Vehicle Assembly Building (VAB)
        ├── Phase2.js        — Launch & Ascent
        ├── Phase3.js        — Trans-Lunar Injection (QTE)
        ├── Phase4.js        — Lunar Surface Operations
        └── Debrief.js       — (ADDED) Mission Debrief / results screen module
Each phase module must export a uniform interface:
JavaScript
export default {
  name: 'PHASE_1',
  enter(params)  { /* build scene, bind inputs, start logic */ },
  update(dt)     { /* per-frame logic, called from main loop */ },
  exit()         { /* dispose geometries/materials, remove listeners, return metrics delta */ }
};
GameState.js drives phase transitions through this interface. No phase may ever reference another phase's internals directly — only through GameState events.
 3.⁠ ⁠GLOBAL APPLICATION STATE & METRICS
Permanently display these four metrics in the top HUD (live, animated count-up/down on change):
Metric 	Starting value 	Display format 
BUDGET	100 (million $ equivalent)	BUDGET: 084.2M
MASS	100 (index)	MASS: 092.6t
POWER	100 (%)	PWR: 087%
SCIENCE	0	SCI: 042
Rule: updateMetric(key, delta, reason) must exist in GameState. Every mutation must log a reason string to the Mission Control Terminal (e.g., MASS −12.0t — STAGE 1 SEPARATION). Metrics animate via GSAP on the HUD numbers.
Consequence Engine (ADDED DEPTH): Decisions in Phase 1 must mathematically affect difficulty and parameters in later phases. Store them in GameState.consequences and consume them in the target phase:
Budget Optimized → Phase 2 driftMultiplier = 1.5 (50% increased randomized rotational force amplitude).
Heavy Payload → Phase 3 burnWindow = 1.2s instead of base 2.0s; also Phase 2 thrust visual intensity reduced.
Science Priority → scienceBonus = +40, but powerDrainPerSecond and budgetDrainPerSecond apply during Phases 2–4.
Stacking rule (ADDED): consequences stack multiplicatively where sensible; document each formula in a comment.
State Persistence (ADDED): Save full GameState to localStorage on every phase exit. On load, offer "RESUME MISSION / NEW MISSION" on the boot screen. Add a "RESET SIMULATION" button in a pause menu that clears storage and reloads.
 4.⁠ ⁠THE FAILSAFE (MANDATORY — HACKATHON CRITICAL)
Implement a global event listener: Shift + P.
When pressed at ANY point in the application:
Instantly abort current phase logic (phase.exit() with force: true).
Dispose all Three.js geometries, materials, and event listeners.
Force transition to the next phase with default/last-known state (never crash on missing data — use fallbacks).
Log !! FAILSAFE ENGAGED — SKIPPING TO NEXT PHASE !! to the Mission Control Terminal and play an anomaly audio cue.
Wrap the whole failsafe in try/catch: if the next phase's enter() throws, fall back to a minimal safe scene with a "SIMULATION RECOVERED" message.
This guarantees the live demo can never get hard-stuck. Add a second failsafe: Shift + R = full simulation reset.
 5.⁠ ⁠AUDIO SYSTEM (AudioManager.js)
Implement a dedicated AudioManager on the Web Audio API:
Master gain node → GainNode → destination. Master volume slider + mute toggle in the top-right HUD.
Engine rumble: procedural low-frequency oscillator + noise buffer, lowpass-filtered. Intensity parameter mapped to simulated altitude/speed during Phase 2.
Stage separation: metallic thump — short noise burst + pitched sine drop.
Mission control: radio static bed (looped filtered noise) + short beep tones (Morse-style "beep-beep") before terminal voice messages.
Lunar ambience: soft wind (slow-modulated noise) + quiet footstep ticks (short filtered noise blips) in Phase 4.
Success cue: two-tone ascending chime. Anomaly/failure cue: descending two-tone + brief static burst.
Countdown: beep per second, double-beep at T-0.
Every single trigger location must carry a comment: // AUDIO TRIGGER: <name> — swap with real buffer: loadBuffer('assets/audio/<name>.wav').
Audio must start only after first user gesture (browser autoplay policy) — trigger AudioManager.init() on the first click/keypress, with a commented note.
 6.⁠ ⁠MISSION CONTROL NARRATIVE LAYER
Persistent bottom-left Mission Control Terminal (glassmorphism, monospace, 1px border):
Scrolling, typewriter-style text (character-by-character reveal, GSAP or rAF-driven).
Calm, concise, technical NASA language. Examples of tone:
"FLIGHT DYNAMICS OFFICER: GUIDANCE IS GO. ALL SYSTEMS NOMINAL."
"CAPCOM: ROGER, EAGLE. YOU ARE GO FOR PDI."
"GUIDANCE: ATTITUDE EXCURSION 4.2 DEGREES — CORRECTING."
Dynamic callouts that react to the player's configuration choices (e.g., choosing Budget Build → "FLIGHT: BUDGET CONFIG CONFIRMED. EXPECT INCREASED TRAJECTORY VARIANCE DURING ASCENT.").
Phase-specific telemetry/status messages injected at defined message injection points (comment each: // MC INJECTION POINT).
Random ambient chatter lines every 8–15 seconds (pool of ≥20 lines) during Phases 2–4.
Optional short pre-launch checklist in Phase 1 (4–5 items, player clicks each): completing it grants +5 SCIENCE and logs confirmation.
Terminal auto-scrolls, keeps last 200 lines, and has a subtle blinking cursor.
 7.⁠ ⁠THE 4-PHASE MISSION LOOP
PHASE 1 — VEHICLE ASSEMBLY BUILDING (VAB)
Scene: 3D workspace with OrbitControls, grid floor, overhead lights.
Rocket: 4 stacked cylinders (Stage 1, Stage 2, Stage 3, Lander) built via the placeholder factory.
UI Dock: Right-side HTML panel listing Stage 1 / Stage 2 / Stage 3 / Lander.
Interaction: Clicking a stage smoothly frames it with the camera using GSAP (animated position + lookAt of camera target, ~1s, ease power2.inOut). Highlight the selected stage (emissive/edge glow).
Configuration Drawer: CSS drawer slides open with 3 mutually exclusive options per stage (can differ per stage — define a matrix):
Config 	Effect 
Budget Optimized	Saves Budget (−cost), Phase 2 drift ×1.5
Heavy Payload	Adds Mass, Phase 3 burn window → 1.2s
Science Priority	+Science, drains Power & Budget over time
Selecting a config updates metrics instantly with animation + terminal log.
Progression: Once all 4 stages are configured, reveal a "NAME YOUR VEHICLE" input field (max 16 chars, validated, default "COLUMBIA") and a "ROLL OUT" button (disabled until named). Roll Out → GSAP transition → Phase 2.
Mission Control Terminal comments on every major decision.
PHASE 2 — LAUNCH & ASCENT
Visuals: Snap camera to a dynamic low-angle launch view. Setup post-processing: EffectComposer + RenderPass + subtle FilmPass (grain) to simulate an analog camera feed, plus mild vignette. Comment the swap point for other passes (e.g., UnrealBloomPass on the beacon later).
Liftoff: "LAUNCH" button → 3-2-1 countdown with audio beeps → ignite: camera shake (perlin-style noise on camera position, amplitude tied to throttle), base smoke particle system (instanced quads or THREE.Points, 100+ particles, spawn rate tied to throttle).
Speed illusion: The rocket stays centered. Move smoke particles AND a scrolling background grid/buildings downward to simulate ascent. Optional: subtle FOV widening with speed.
Attitude minigame: Over ~10 seconds of simulated ascent, apply randomized rotational forces to the rocket's Z-axis rotation. Player counters with A / D keys (InputManager). Show a live attitude indicator (HUD ladder/bar) and a drift force readout in terminal.
Failure: If |rotation.z| exceeds 30° for 5 continuous seconds → anomaly cue, red flash, terminal failure message, offer RESTART ASCENT (Phase 2 resets cleanly, metrics restored to phase-entry snapshot).
Stage separation at T+10s: Audio cue + visual flash + bottom cylinder detaches (GSAP tween downward + fade/dispose) → Mass HUD updates (−stage mass) → terminal: "FLIGHT: STAGE 1 SEPARATION CONFIRMED. IGNITION SECOND STAGE." → transition to Phase 3.
Engine audio intensifies monotonically through the ascent.
PHASE 3 — TRANS-LUNAR INJECTION (QTE)
Visuals: Clear the 3D scene entirely (dispose everything from Phase 2). Bring up a 2D HTML/CSS radar overlay: Earth (bottom-left circle), Moon orbit ring, curved transfer path (SVG or CSS-drawn), a trajectory dot traveling along the path.
Mechanic: When the dot reaches the alignment marker (Moon), the "BURN" button flashes red and a terminal callout fires: "GUIDANCE: TLI BURN WINDOW OPEN."
Window math: Player has exactly 2.0s to click. If Heavy Payload was chosen in Phase 1 → 1.2s. Show a shrinking radial/bar timer.
Hit: success chime, terminal confirms injection, metrics update (−fuel mass), proceed.
Miss: anomaly cue, terminal "FLIGHT: BURN WINDOW MISSED. RECYCLING TO TLI ATTEMPT." → clean restart of Phase 3 only (consequence window stays at 1.2s — the penalty persists).
PHASE 4 — LUNAR SURFACE OPERATIONS
Scene: Flat Plane (lunar surface, gray monochrome, subtle noise texture via canvas-generated texture), dark starfield (THREE.Points, 800+ stars), low directional "sun" light + ambient.
Astronaut: Placeholder Box mesh, third-person follow camera (smoothed with GSAP/damped lerp).
Controller: WASD movement relative to camera, Spacebar jump. Custom gravity constant: lunar gravity = Earth/6 (~1.62 m/s²) for jump/fall. Slight floaty air-control. Footstep audio on landing.
Objective: A glowing beacon (THREE.Points + PointLight, gentle pulse via GSAP) placed ~30 meters away. Navigate to it; HUD shows live distance readout (RANGE: 018.4m).
Confirm: Within 2m, prompt "PRESS E TO CONFIRM LANDING SITE" → pressing E triggers success sequence: light flares, terminal narrates final approach confirmation, transition to Debrief.
Soft lunar audio environment throughout.
MISSION DEBRIEF
Cinematic results panel drops down (GSAP):
Vehicle name
Final metrics: Budget, Mass, Power, Science
Overall rating A–F, computed by a transparent rubric (ADDED): score = weighted sum of normalized metrics + performance bonuses (no Phase restarts = +10, checklist done = +5). Comment the formula. A ≥90, B ≥75, C ≥60, D ≥45, F <45.
Formal post-flight narrative summary (3–5 sentences) generated from the player's choices and performance, written in official NASA post-flight report style, assembled from template fragments keyed to which configs were chosen, whether failures occurred, and final rating. E.g., Heavy Payload + missed burn → "The decision to prioritize payload mass imposed significant guidance constraints during translunar injection; the initial burn attempt was recycled successfully…"
Buttons: "NEW SIMULATION" (reset) and "VIEW LOG" (replays the full Mission Control transcript in a scrollable overlay).
 8.⁠ ⁠ENGINEERING DEPTH REQUIREMENTS (ADDED — WHAT MAKES THIS INSANE)
A. Deterministic simulation seed. Use a seeded PRNG (mulberry32) for all randomness (drift forces, chatter timing). Seed shown in a debug footer. Allows reproducible demos and bug reports.
B. Delta-time game loop. Everything physics-ish (drift, gravity, particles) uses dt from a clock, capped at 50ms to avoid spiral-of-death after tab-switch. Fixed-substep for the attitude integrator if needed.
C. Centralized InputManager. Single keydown/keyup map with action names (thrust_left, jump, confirm), re-bindable via a KEYMAP constant at the top. Phases query Input.isDown('thrust_left') — no scattered event listeners except in InputManager and the failsafe.
D. Pause system. Esc opens a translucent pause overlay: pauses the rAF update (but not render), freezes timers (burn window, countdown) — implement a GameState.paused gate in main loop and every phase timer. Pause menu: Resume, Restart Phase, Reset Simulation, Audio volume, Failsafe hint.
E. Telemetry depth. Beyond the 4 metrics, stream realistic fake telemetry in the terminal/HUD during Phases 2–3: altitude, velocity, downrange, fuel %, g-force — updated every 200ms from phase logic (commented as // TELEMETRY FEED).
F. Performance budget. ≤ 60 draw calls per phase, particle systems via instancing, dispose pattern (geometry.dispose(), material.dispose()) mandatory in every exit(). Cap pixel ratio at 2. Target 60fps on integrated graphics.
G. Error resilience. Every phase enter() wrapped in try/catch in GameState; failures degrade to a "SAFE MODE" screen offering failsafe skip. Console errors must never white-screen the demo.
H. Debug overlay (toggle Shift + D). FPS, draw calls, triangles, current phase, active consequence flags, seed. Hidden by default.
I. Mission Elapsed Time (MET) clock in HUD from Phase 2 onward, format T+ 002:14:33.
J. Accessibility & polish. All buttons keyboard-focusable, aria-labels on HUD clusters, prefers-reduced-motion respected (disable camera shake/grain), HUD legible at 1366×768 and 4K (use clamp()/vw units), no text below 12px.
K. Keyboard shortcut legend (toggleable overlay, ? key): A/D attitude, WASD move, Space jump, E confirm, Esc pause, Shift+P failsafe, Shift+R reset, Shift+D debug, M mute.
L. Comment density standard. Heavily comment, especially at: asset loading / GLB swap points, audio trigger locations, Mission Control message injection points, phase transitions, failsafe logic, consequence formulas, and the debrief rubric. Comment style: // ─── SECTION ─── banners + inline // WHY: notes on non-obvious math.
 9.⁠ ⁠UI SPECIFICATION (style.css)
Strict monochrome: #000 base, #fff lines/text, grays via rgba(255,255,255,0.06–0.6) fills only. No color anywhere except the single red reserved for anomaly/burn-window flashes (#ff2b2b).
Glassmorphism: background: rgba(0,0,0,0.55); backdrop-filter: blur(10px); border: 1px solid rgba(255,255,255,0.9);
HUD: top bar (metrics + MET + volume/mute), bottom-left terminal, bottom-center context prompts, right dock (Phase 1), attitude ladder (Phase 2), radar overlay (Phase 3), range readout (Phase 4).
Typography: Roboto Mono for data, Inter for headings; letter-spaced uppercase labels; thin 1px dividers; 2–4px corner ticks on panels for the "flight console" feel.
GSAP-animated: metric pulses, drawer slides, panel drops, button states (hover = fill inversion), boot/loading sequence with progress readout.
10.⁠ ⁠BOOT SEQUENCE
index.html loads → black boot screen: APOLLO SIMULATION SYSTEMS — INITIALIZING with a typed progress log (renderer, audio, state, phases) → "RESUME MISSION" if save exists, else "NEW SIMULATION" → Phase 1. First user gesture initializes AudioManager.
11.⁠ ⁠EXECUTION PROTOCOL
Write the complete codebase now, in this order:
package.json (three, gsap, vite deps) and vite.config.js
index.html and style.css
Core architecture: main.js, GameState.js, SceneManager.js, UIManager.js, AudioManager.js, InputManager.js
All four phase modules + Debrief.js inside src/phases/, each implementing the uniform phase interface
README.md
Constraints: no TODOs left unresolved, no pseudo-code — everything runnable with npm install && npm run dev. Placeholder geometry must be visually clean (proper proportions, stacked correctly, subtle shading). The final result must feel like a serious, polished, demo-reliable aerospace simulation application — precise, professional, monochrome, and unbreakable on stage.
What I added on top of both originals (so you know what's new)
Since you asked me to go deep, here's the layer I engineered in beyond a simple merge:
Uniform phase interface (enter/update/exit) — this is the single biggest stability upgrade. Both original prompts said "modular" but never defined the contract between phases. Without this, the AI will spaghetti-call across modules and break by Phase 3.
InputManager — both prompts scatter A/D/WASD/E listeners across phases; centralizing prevents double-binding and the classic "keys stuck after phase transition" bug.
Pause system + dt-based loop + MET clock — original prompts had timers (countdown, burn window) with no pause story; any tab-switch during the demo would corrupt timing.
Seeded RNG + debug overlay — makes the demo reproducible and debuggable live on stage.
Consequence stacking rule + per-config matrix — original prompts implied consequences but left the math ambiguous; ambiguity is where AI-generated code silently breaks.
Persistence (localStorage) + Reset — makes the hackathon demo resilient across browser crashes.
Safe-mode try/catch wrapper + double failsafe (Shift+P / Shift+R) — the original failsafe had no fallback if the next phase crashed.
Grading rubric formula + fragment-based report generator — makes the A–F rating and post-flight report actually deterministic and codeable instead of hand-waved.
Performance budget + dispose discipline — Phase 2 particles + Phase 3 scene clears are exactly where memory leaks kill a live demo.
Full shortcut legend, accessibility, reduced-motion — polish that reads as "professional software" to judges.



this this give by the ai i also provide you the multiple ai response to my  game and how to do alysies thiand twst this alol in this debugs first properly.. this is my whole planing and what i want read this first everly time and and accoerding to this use all the resources with aprropriate manner and make the best out of best  game possible to build ... 

Apollo 11 Saturn V Mission Simulator — Architecture Blueprint
Your prompt got cut off mid-sentence (import via ES mo...), so before I dump 5,000+ lines of code into a chat window that will inevitably truncate, break formatting, or produce untested fragments, let me be direct about what actually works for a 30-hour hackathon.

The honest reality check
What you're asking for — a "production-ready, modular, high-fidelity" NASA-grade simulator with authentic telemetry — is a 3–6 month project for a team of 3+. No single LLM response will produce a working, demo-reliable codebase of that scope. Anyone who claims otherwise is shipping you subtly broken code that fails at hour 28 of your hackathon.

What is achievable in 30 hours by a focused dev (or small team) with AI assistance:

A polished Saturn V 3D model (loaded GLB) with stage separation animation

A mission timeline scrubber (T-minus to T+ lunar landing)

A telemetry dashboard (altitude, velocity, fuel, attitude) driven by a keyframed data array

Cinematic camera modes (ground tracking, chase, cockpit, Earth-rise)

Mission-control UI aesthetic (monospace, grid layouts, green/amber CRT vibes)

Sound design (engine rumble, countdown, radio callouts)

That's a great demo. "Authentic NASA telemetry simulation" is not.

What I recommend instead
Let me build you a real, working scaffold you can extend — actual files, actual code, tested patterns. I'll deliver it in structured chunks so nothing truncates:

Chunk 1 — Project skeleton & data model
text
/apollo11-sim
├─ index.html
├─ vite.config.js
├─ package.json
├─ /src
│  ├─ main.js
│  ├─ /core
│  │  ├─ SceneManager.js
│  │  ├─ CameraDirector.js
│  │  ├─ Clock.js          ← mission elapsed time
│  │  └─ InputController.js
│  ├─ /world
│  │  ├─ SaturnV.js        ← GLB loader + stage hierarchy
│  │  ├─ Earth.js
│  │  ├─ Moon.js
│  │  ├─ LaunchPad.js
│  │  └─ Starfield.js
│  ├─ /sim
│  │  ├─ MissionTimeline.js ← keyframed events
│  │  ├─ Telemetry.js       ← interpolated data feed
│  │  └─ missionData.json   ← the "flight plan"
│  ├─ /ui
│  │  ├─ Dashboard.js
│  │  ├─ Timeline.js
│  │  ├─ CameraBar.js
│  │  └─ styles.css
│  └─ /audio
│     └─ AudioManager.js
Chunk 2 — missionData.json (the heart of "realism")
Instead of a physics sim, use keyframed telemetry at ~50 mission events (ignition, tower clear, Max-Q, S-IC sep, S-II ignition, parking orbit, TLI, lunar orbit, PDI, touchdown). Each entry:

json
{
  "met": -10, "label": "Terminal Count",
  "altitude": 0, "velocity": 0, "thrust": 0,
  "stage": "S-IC", "fuelPct": 100,
  "camera": "pad"
},
{
  "met": 12, "label": "Tower Clear",
  "altitude": 85, "velocity": 50, "thrust": 34000,
  "stage": "S-IC", "fuelPct": 96,
  "camera": "chase"
}
Interpolate between keys for smooth telemetry. This is how real mission sims look authentic without needing a physics engine.

Chunk 3 — Core Three.js scene + Saturn V loading
Chunk 4 — UI dashboard + timeline scrubber
Chunk 5 — Camera director & cinematic transitions
Chunk 6 — Audio + polish + deploy
Two decisions I need from you before writing code
Saturn V model: Do you have a GLB/GLTF asset, or should I code a procedural low-poly Saturn V (cylinder stages + conical adapter + escape tower)? Procedural is guaranteed to work in 30h; asset loading depends on what you can find/license in time.

Scope confirmation: Given the honest reality above, do you want me to (a) proceed with the achievable scaffold I described, or (b) attempt the full high-fidelity build knowing it won't be production-ready or demo-safe?

Reply with those two answers and I'll start delivering Chunk 1 immediately — real files, tested patterns, no placeholder // TODO: implement physics here cop-outs. I'd rather give you 6 chunks of code that runs than 1 giant message that doesn't.

Apollo 11 Saturn V Mission Simulator — Master Prompt v2 (Production-Ready)
Here's the corrected, tightened, and hardened version of your prompt. I've fixed the truncation, added the missing pieces (asset contract, acceptance criteria, failure modes, deliverable format), and removed the parts that make AIs hallucinate (vague words like "professional-grade" without measurable definitions).

What I changed and why:

Your version	Problem	My fix
Cut off mid-sentence	AI can't respond to an incomplete spec	Completed the tech stack section
"Production-ready, professional-grade"	Unmeasurable — every AI interprets differently	Replaced with concrete acceptance criteria (FPS targets, load times, test checklist)
No asset contract	AI will invent filenames and break at runtime	Defined exact asset paths, formats, and fallbacks
No output format	AI dumps one giant message → truncation	Required chunked delivery with file-per-block format
"High-fidelity" unqualified	AI will fake physics or skip it	Locked to keyframed telemetry (no physics engine) — this is what actually ships
No scope boundary	AI will try to build 6 months of work in one response → broken code	Explicit in-scope / out-of-scope lists
THE PROMPT (copy everything below this line)
MASTER PROMPT — APOLLO 11 SATURN V MISSION SIMULATOR (v2)
1. ROLE
You are a Senior Three.js Engineer + Simulation Systems Architect. You write code that runs on the first try. You do not invent APIs. You do not skip error handling. You deliver in tested, modular chunks — never one giant response that truncates.

2. PROJECT
A web-based interactive Apollo 11 Saturn V mission simulator for a 30-hour hackathon demo. It must look and feel like authentic NASA mission control software — dark UI, monospace telemetry, precise readouts, cinematic 3D views. It is a visualization/simulation tool, not a game and not a real physics simulator.

3. TECH STACK (locked — do not deviate)
Build: Vite 5+, Vanilla JS, ES Modules (type: "module")

3D: Three.js r160+ via import * as THREE from 'three' (npm, not CDN)

Controls: three/examples/jsm/controls/OrbitControls.js

Loaders: GLTFLoader, DRACOLoader from three/examples/jsm/

UI: Plain HTML/CSS in index.html + DOM manipulation. No React, Vue, or Svelte.

State: A single SimState object + event emitter pattern. No Redux/Zustand.

Data: JSON files imported via fetch() from /public/data/

Audio: Native HTMLAudioElement + Web Audio API for gains/fades

No external dependencies beyond Three.js and Vite.

4. ASSET CONTRACT (files the user WILL provide)
The user has these assets ready. Reference them at exactly these paths. If a file is missing at runtime, log a warning and use the procedural fallback — never crash.

text
/public/assets/models/saturn-v.glb          # Full Saturn V, stages as named nodes
/public/assets/models/earth.glb             # optional; else procedural sphere
/public/assets/models/moon.glb              # optional; else procedural sphere
/public/assets/textures/
  ├─ earth-day.jpg
  ├─ moon-color.jpg
  ├─ stars-milkyway.jpg
  └─ launchpad-concrete.jpg
/public/assets/audio/
  ├─ countdown.mp3
  ├─ engine-rumble.mp3
  ├─ stage-separation.mp3
  └─ radio-callouts.mp3
/public/data/missionData.json                # keyframed telemetry (see §7)
Expected named nodes in saturn-v.glb:
S_IC, S_II, S_IVB, 
LM
, 
CSM
, escape_tower, fins[], engines[].
If nodes are missing, treat the whole GLB as a single rigid object and log [ASSET WARNING] missing node: <name>.

5. ARCHITECTURE (required file tree)
text
apollo11-sim/
├─ index.html
├─ package.json
├─ vite.config.js
├─ /public
│  ├─ /assets/{models,textures,audio}/...
│  └─ /data/missionData.json
└─ /src
   ├─ main.js
   ├─ /core
   │  ├─ SceneManager.js      # renderer, scene, lights, resize, RAF loop
   │  ├─ CameraDirector.js    # camera modes + smooth transitions
   │  ├─ MissionClock.js      # MET (mission elapsed time), speed control, pause
   │  └─ EventBus.js          # tiny pub/sub
   ├─ /world
   │  ├─ SaturnV.js           # GLB load + stage hierarchy + separation logic
   │  ├─ Earth.js
   │  ├─ Moon.js
   │  ├─ LaunchPad.js
   │  └─ Starfield.js
   ├─ /sim
   │  ├─ TelemetryEngine.js   # interpolates between keyframes in missionData.json
   │  └─ MissionEvents.js     # fires discrete events (ignition, sep, etc.)
   ├─ /ui
   │  ├─ Dashboard.js         # telemetry readouts
   │  ├─ Timeline.js          # scrubber (T-minus … T+landing)
   │  ├─ CameraBar.js         # camera mode buttons
   │  ├─ EventLog.js          # scrolling mission log
   │  └─ styles.css
   └─ /audio
      └─ AudioManager.js
6. SIMULATION MODEL (critical — read carefully)
No physics engine. No orbital mechanics solver. Simulation is driven by keyframed telemetry interpolation, which is how real mission-replay tools actually work.

missionData.json contains ~50–80 keyframes from MET -600s (T-10 min) through MET +102h 45m (lunar landing).

Each keyframe: { met, label, altitudeKm, velocityMs, thrustKN, stage, fuelPct, pitchDeg, camera }.

TelemetryEngine.tick(met) finds the two surrounding keyframes, linearly interpolates numeric fields, and emits the result on the EventBus.

Discrete events (MissionEvents.js) fire once when MET crosses their keyframe: towerClear, maxQ, stageSep(S_IC), TLI, lunarOrbitInsert, PDI, touchdown.

Timeline scrubber lets the user jump MET ±. Playback speed: 0.1×, 1×, 10×, 100×, 1000×.

7. missionData.json SCHEMA (must implement exactly)
json
{
  "meta": { "name": "Apollo 11", "launchUTC": "1969-07-16T13:32:00Z" },
  "keyframes": [
    {
      "met": -10,
      "label": "Terminal Count",
      "altitudeKm": 0,
      "velocityMs": 0,
      "thrustKN": 0,
      "stage": "S-IC",
      "fuelPct": 100,
      "pitchDeg": 90,
      "camera": "pad"
    },
    {
      "met": 12,
      "label": "Tower Clear",
      "altitudeKm": 0.085,
      "velocityMs": 50,
      "thrustKN": 34000,
      "stage": "S-IC",
      "fuelPct": 96,
      "pitchDeg": 88,
      "camera": "chase"
    }
  ]
}
Populate this file with real Apollo 11 flight data (public NASA sources) at minimum for: T-10s, ignition, liftoff, tower clear, Max-Q, S-IC sep, S-II ignition, S-IVB ignition, parking orbit, TLI, lunar orbit, PDI, touchdown. Intermediate points can be linear interpolations.

8. CAMERA MODES (minimum required)
Pad Cam — fixed at launch pad base, looking up

Chase Cam — follows Saturn V at fixed offset, damped

Onboard — first-person from stage 2 looking down at Earth

Ground Track — wide orbital view of Earth below

Lunar — Moon-centered view during translunar coast

Free — OrbitControls, user-driven

Each mode transition must lerp over ≥ 1.2s using THREE.Vector3.lerp + Quaternion.slerp. No snap cuts.

9. UI REQUIREMENTS (mission-control aesthetic)
Palette: #0a0e14 bg, #00ff9d accent, #ffb000 warnings, #ff3b30 alarms

Typography: monospace ('JetBrains Mono', 'Consolas', monospace)

Grid layout — three columns: left = telemetry panel, center = 3D canvas, right = event log

Bottom bar — timeline scrubber + speed buttons + play/pause

Top bar — mission clock T+ 00:00
:00
, mission name, current stage indicator

Telemetry readouts must update every frame with no layout thrash (write only textContent)

All numbers fixed-width — pad with padStart() so the UI doesn't jitter

10. PERFORMANCE BUDGET (acceptance criteria)
60 FPS on a 2020-era laptop at 1920×1080 with shadows ON

Initial load under 4s on 20 Mbps (LCP)

GLB size ≤ 15 MB (if larger, warn user at build time)

Zero console errors at runtime

≤ 3 draw calls per Saturn V stage (use MeshStandardMaterial, merge where possible)

11. ERROR HANDLING (non-negotiable)
Wrap every GLTFLoader call in try/catch; on failure, log [ASSET ERROR] and instantiate a procedural fallback (cylinder-stack Saturn V, basic spheres for Earth/Moon).

If missionData.json fails to load, ship a hardcoded minimal keyframe array so the demo never goes blank.

Show a visible banner in the UI if any asset fails: ⚠ Degraded mode: <asset> missing.

No unhandled promise rejections. No console.error without a UI fallback.

12. OUT OF SCOPE (do not build)
Real orbital mechanics / n-body physics

Multiplayer / networking

Save/load mission states

Mobile touch controls (desktop-only demo)

VR mode

Any backend / server code

13. DELIVERY FORMAT (critical — follow exactly)
Do NOT deliver one giant response. Deliver in ordered chunks. Each chunk:

Starts with ### CHUNK N — <title>

Lists exactly which files it contains

For each file, a fenced code block with the full file path as the first line:

text
**File: `src/core/SceneManager.js`**
```js
// full file contents here
```
Ends with ✅ Chunk N complete. Reply "continue" for Chunk N+1.

Required chunks:

#	Contents
1	package.json, vite.config.js, index.html, src/main.js, src/core/EventBus.js
2	src/core/SceneManager.js, src/core/MissionClock.js
3	src/world/Starfield.js, src/world/Earth.js, src/world/Moon.js, src/world/LaunchPad.js
4	src/world/SaturnV.js (GLB load + fallback + stage hierarchy)
5	src/sim/TelemetryEngine.js, src/sim/MissionEvents.js, public/data/missionData.json (real Apollo 11 data)
6	src/core/CameraDirector.js (all 6 camera modes)
7	src/ui/Dashboard.js, src/ui/Timeline.js, src/ui/CameraBar.js, src/ui/EventLog.js, src/ui/styles.css
8	src/audio/AudioManager.js + final main.js wire-up + README with run instructions
14. CODING STANDARDS
ES2022, 
const
/let, arrow functions, JSDoc on every exported function

Every module exports a named class or object — no default-export soup

No var, no ==, no alert()

Class constructors accept a single opts object

Disposal methods (dispose()) on every class that allocates Three.js resources

requestAnimationFrame loop lives only in SceneManager — everything else subscribes to SceneManager.onUpdate(cb)

15. START
Reply first with: "Ready. Confirmed stack, asset contract, and 8-chunk plan. Reply 'start' to receive Chunk 1."

Wait for user to say start before emitting Chunk 1.

How to use this
Paste §1–§15 into any capable model (Claude, GPT-4+, Gemini 2.5+, DeepSeek).

When it says "Ready," reply start — do not let it bulk-dump.

After each chunk, verify the files parse (node --check for JS files) before saying continue. If a chunk is broken, paste the error back and ask for a repair.

If the model tries to skip the chunk protocol, reply: "Follow §13 delivery format exactly."

What I corrected vs. your original prompt
Removed "high-fidelity" as an unmeasurable goal — replaced with §6 keyframe model (what actually ships in 30h)

Added the asset contract (§4) — the single biggest source of runtime crashes in AI-generated Three.js code

Added acceptance criteria (§10) — makes "production-ready" testable

Added error handling (§11) — ensures the demo survives missing assets at the hackathon

Locked out-of-scope (§12) — prevents the AI from scope-creeping into physics engines it can't finish

Enforced chunked delivery (§13) — the reason your first prompt failed was implicit "give me everything" → truncation

Added §15 handshake — forces the AI to confirm understanding before wasting output tokens on a wrong interpretation

Say the word and I'll also write the first 3 chunks myself as a reference implementation so you can hand the rest to any model and it just fills in the pattern.

# Apollo 11 Simulator — Full Reference Implementation

All 8 chunks. Every file works. `npm install && npm run dev` runs it. Tested patterns throughout. The Saturn V uses a **procedural fallback** so the demo never crashes if the GLB isn't ready — drop your GLB in `public/assets/models/saturn-v.glb` later and it picks it up automatically.

---

## ### CHUNK 1 — Project skeleton

// **File: `package.json`**

```markdown
# Apollo 11 — Saturn V Mission Simulator

Real-time interactive mission visualizer with authentic NASA-style mission control UI.

## Quick start

```

Open http://localhost:5173.

## Controls

- **Timeline scrubber** — drag to any point from T−10s to T+102h (lunar landing)
- **Speed buttons** — 0.1×, 1×, 10×, 100×, 1000× (use 100×/1000× to skip to lunar events)
- **Play/Pause** — freeze or resume mission clock
- **Camera modes** — PAD, CHASE, ONBOARD, GROUND, LUNAR, FREE (orbit with mouse in FREE)

## Optional assets

Drop these files to enhance visuals/audio. The simulator runs fine without them — it uses procedural fallbacks:

```
public/assets/models/saturn-v.glb          # textured rocket
public/assets/audio/engine-rumble.mp3      # loop during ascent
public/assets/audio/stage-separation.mp3   # one-shot at staging
public/assets/audio/countdown.mp3          # plays at T-10
```

If `saturn-v.glb` is present, stage nodes named `S_IC`, `S_II`, `S_IVB`, `tower` will be used for staging separation. Otherwise, procedural stages are used.

## Architecture

- `src/core/` — scene, clock, camera, event bus
- `src/world/` — 3D objects (Saturn V, Earth, Moon, pad, stars)
- `src/sim/` — telemetry interpolation + discrete mission events
- `src/ui/` — dashboard, timeline, camera bar, event log
- `public/data/missionData.json` — the flight plan (edit to change the mission)

## Editing the mission

`public/data/missionData.json` contains the keyframed flight plan. Each entry:

```json
{ "met": 0, "label": "Liftoff", "altitudeKm": 0, "velocityMs": 0, "thrustKN": 34000, "stage": "S-IC", "fuelPct": 100, "pitchDeg": 90, "camera": "chase" }
```

`met` is seconds from liftoff (negative = before). Numeric fields interpolate between keyframes. `stage`, `label`, `camera` snap to the nearest upcoming keyframe.

## Notes

- Keyframed simulation, not physics — this is a mission-replay tool, not a KSP-style simulator.
- Staging hides stage meshes at the correct MET (S-IC at T+165, S-II at T+549, escape tower at T+160).
- Missing `missionData.json` falls back to a 2-keyframe hardcoded plan — the demo never goes blank.
```

✅ Chunk 8 complete.

---

## What you have now

Run `npm install && npm run dev`. You get:

- Saturn V on a launch pad at KSC, engine plume firing during ascent
- Six camera modes with smooth 1.2s transitions  
- Live telemetry (altitude, velocity, thrust, pitch, stage, fuel) driven by real Apollo 11 keyframes
- Timeline scrubber + play/pause + speed 0.1×–1000×
- Mission event log that fires at the correct MET
- Atmosphere fade (blue haze → black space) as altitude increases
- GLB auto-swap if you drop `saturn-v.glb` into the assets folder
- Zero crashes if any asset is missing

**Demo flow for the hackathon:** start at T−10, hit play, watch the launch with pad cam → switch to chase at T+12 → onboard for Max-Q → scrub the timeline to TLI and hit LUNAR cam → scrub to T+369940 and let the event log announce *"The Eagle has landed."*

To extend: edit `missionData.json` for more keyframes, drop textures in `public/assets/textures/` and swap the procedural materials in `Earth.js`/`Moon.js` for `TextureLoader` calls, or wire in a GLB for `Earth` / `Moon`. The whole thing is < 900 lines.


this is only for reference purpone thigs about it and use this . but not total this this is just a refeerence how to make what to makehow its work and things .. 