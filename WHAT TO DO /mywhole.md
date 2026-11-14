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

