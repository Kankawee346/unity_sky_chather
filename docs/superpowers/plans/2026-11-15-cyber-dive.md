# CYBER//DIVE Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable 30–45 second Windows vertical slice of CYBER//DIVE, including a separate Unity project, boss fight, parry, monochrome visuals, and two-board input integration.

**Architecture:** Pure C# rules own scoring, combat and pose arming. Unity MonoBehaviours render and advance the simulation using DSP time and real-time device timeouts. A separate Node bridge accepts existing ESP32 CSV and versioned Pi JSON over MQTT, exposing loopback HTTP snapshots to Unity.

**Tech Stack:** Unity 6000.5.2f1, Universal 3D URP, Input System, uGUI/TextMeshPro, Cinemachine, ProBuilder, VFX Graph, Node.js with Aedes, Python paho-mqtt for Pi examples.

**Spec:** `docs/superpowers/specs/2026-11-15-cyber-dive-design.md`

## Global Constraints

- New project `CyberDive`, independent bridge `CyberDiveBridge`; do not change CatchLab, CatchLabFriend, ControllerBridge, or existing firmware.
- Windows x64; initial slice 30–45 seconds, subsequent full mission approximately two minutes.
- ESP32 FSR/Joystick controls horizontal movement; Pi camera provides an optional parry ability.
- Camera player: one person, one configurable free arm, wrist above shoulder. No Pi rendering or continuous video upload.
- HULL 3; parry window 0.6 seconds; cooldown 2 seconds; maximum score multiplier x8. Treat balance as tuneable configuration.
- Pose JSON v1 with sessionId, seq, gesture, active, confidence; approximately 10 Hz, qos=0, retain=false, stale after 500 ms using bridge receive time.
- Keep `catchlab/controller` CSV and `catchlab/pose` MQTT topics. Broker TCP 1883 on configured LAN IP; HTTP loopback 127.0.0.1:8787.
- New project packages installed through Package Manager Client API or Package Manager UI, never manual manifest edits.
- Gameplay collisions must not rely on VFX particles. Quality controls must reduce visual particles and disable flash/glitch/shake.
- Target 1080p 60 fps is a measurement goal, not a guarantee before profiling the user's machine.
- No multiplayer, cloud persistence, live music analysis, or free-flight system in this slice.
- Workspace currently has no parent Git repository. Do not pretend to commit or create a worktree. Decide local project version control at execution; no remote creation or publishing is authorized.

## Review Focus

1. Pi restarts with seq reset while holding its arm: session must change, no automatic skill on reconnect. Test in Tasks 1, 4, 5.
2. Wi-Fi drops during a held gesture: invalidate the pose and disarm; movement must return to neutral. Test in Tasks 4, 5.
3. Pause/resume, death and restarting: old pose and buffered skill must not trigger. Test in Tasks 1, 2, 5.
4. HTTP stalls and game time slows: input age must use a monotonic real-time clock, independent of gameplay time. Test in Tasks 4, 5.
5. Many effects or reduced visual settings: collisions and readable dodge lanes must remain unchanged. Test and profile in Tasks 3, 6.

---

## File Structure

`CyberDive/Assets/CyberDive/Core/` contains pure rules and an assembly definition.
- `RunState.cs`: mission, HULL, score, SYNC, parry, damage and win/loss rules.
- `PoseGate.cs`: initial-neutral requirement, edges, revision/session dedupe and re-arming.
- `CombatSettings.cs`: serializable balance parameters.

`CyberDive/Assets/CyberDive/Runtime/` contains scene behavior.
- `DiveGame.cs`: stage orchestration and DSP clock timeline.
- `PilotInput.cs`: A/D, Space, device direction and pause handling.
- `BridgeClient.cs`: bounded HTTP polling and immediate stale resets.
- `ShipView.cs`, `EnemyWave.cs`, `ProjectilePool.cs`, `WardenBoss.cs`: visual/physical combat entities.
- `TerminalHud.cs`, `AudioDirector.cs`, `QualityControls.cs`: UI, original generated/synthesized sound, effect switches.

`CyberDive/Assets/CyberDive/Art/` contains shader/material/VFX assets.
- `DitherHull.shader`: white ordered-dither mesh shading.
- `ParryShield.shader`: translucent cyan defense indication.
- `Effects/`: generated Unity particle/VFX assets with no gameplay responsibility.

`CyberDive/Assets/CyberDive/Editor/` contains `ProjectSetup.cs` (Editor-generated scene, renderer, assets, player configuration), `PackageInstaller.cs`, `BuildPlayer.cs`, and `CapturePreview.cs`.

`CyberDive/Assets/CyberDive/Tests/EditMode/` and `PlayMode/` contain separate test assemblies. Scene `Assets/CyberDive/Scenes/Dive.unity` must be produced using Editor APIs and saved with metadata.

`CyberDiveBridge/` contains `package.json`, `pose-store.cjs`, `controller-store.cjs`, `server.cjs`, matching `*.test.cjs`, `mqtt.integration.test.cjs`, `.env.example`, and `tools/pose-simulator.py`.

`CyberDive/README_TH.md` teaches opening, playing and building. `CyberDiveBridge/PROTOCOL_TH.md` is the contract to send to the friend.

### Task 1: Project and deterministic gameplay rules

**Files:** Core files, EditMode tests, package installer, project settings created through Editor APIs.

**Interfaces:**
- `RunState.Start(double now)`, `Tick(double now)`, `TryParry(double now): bool`, `Damage(double now): bool`, `AwardKill(int basePoints)`, `Reflect(double now): bool`, `DefeatBoss()`, `Pause(bool paused, double now)`.
- Read-only `Hull`, `Score`, `Multiplier`, `Sync`, `Phase`, `ShieldActive`, `CooldownRemaining`, `OverdriveActive`; mission phase enum Ready/Playing/Paused/Won/Lost.
- `PoseGate.Observe(string sessionId, long revision, string gesture, bool active, bool fresh): bool`; true is a single new parry intent. `Reset()` disarms until neutral.
- Renderer and bridge cannot mutate score/HULL fields directly.

- [ ] Verify CLI, installed Editor and template ID `com.unity.template.urp-blank`; create `CyberDive` without cloud linkage. Add Pipeline before first Editor open when available.
- [ ] Install compatible Cinemachine, ProBuilder, VFX Graph through Package Manager; use existing URP version resolved by template. Record actual versions. Do not use the 2D renderer.
- [ ] Write failing rule tests: initial HULL=3; parry at t=1 accepted, t=1.2 rejected, t=3 accepted; shield expires at t=1.6; pause freezes gameplay timers; damage during invulnerability does not stack; third valid hit loses; boss defeat wins only while playing; terminal phases reject scores/skills; restart clears cooldown and score; multiplier caps at 8.
- [ ] Write failing gate tests: first active after startup/restart does not fire; NONE then PARRY fires exactly once; held active and repeated revision do not fire; stale/reset/session change disarms; a fresh neutral from the new session restores arming.
- [ ] Run Unity EditMode tests and confirm failures are missing behavior, then implement pure rules. Run again; expected all rule tests PASS, zero compilation errors.
- [ ] Record test result and package versions in an execution ledger beside the plan. If a local Git repository is established, commit only new project sources and metadata, excluding Library/Temp/Build/UserSettings.

### Task 2: Playable combat slice

**Files:** DiveGame, PilotInput, ShipView, EnemyWave, ProjectilePool, WardenBoss, ProjectSetup, PlayMode combat tests.

**Interfaces:** `PilotInput.Direction: float`, `ParryPressed: bool`; `DiveGame.State: RunState`; `ProjectilePool.Spawn(position, velocity, reflectable)`; game owns `ApplyHit()` and `ApplyReflection()` calls into RunState.

- [ ] Write failing PlayMode tests for travel bounds, automatic shots destroying a target, inactive pooled projectiles not hitting, reflectable bullets reflecting while shield is active, obstacles still damaging, and pause preventing damage/spawns.
- [ ] Create scene via Editor APIs: perspective camera behind ship, horizontal bounds, original geometric ship, world depth, one drone type and boss with a real damageable collider.
- [ ] Schedule a 40-second slice: short briefing, 10-second wave, boss arrives, boss defeat wins; at 40 seconds unresolved boss loses. Use deterministic patterns with safe lanes and visible parry warnings.
- [ ] Implement automatic forward fire, pooling, health/damage, scoring through Task 1 interfaces, A/D and Space input, Escape pause and restart button. Use DSP elapsed time with pause offset for scheduling.
- [ ] Run PlayMode tests. Expected PASS; manually play through win and loss, then restart without leftover projectiles, targets or score.
- [ ] Add an original procedural metronome/percussion pulse aligned to the DSP timeline for slice timing; prevent repeated audio playback after resume/restart. Record that this is prototype audio, not final soundtrack.

### Task 3: Point-cloud visual treatment and HUD

**Files:** Art shaders/materials, effect assets, TerminalHud, QualityControls, camera setup and CapturePreview.

**Interfaces:** HUD reads DiveGame.State and BridgeClient status; visual events consume combat results without modifying them. `QualityControls` exposes particle density and independent flash/glitch/shake flags.

- [ ] Add geometric ship/boss silhouette and ordered-dither mesh shader first; inspect in running scene. Keep unambiguous bullet outlines, near-to-far depth and black negative space.
- [ ] Add cyan shield ring, reflection trails and modest VFX particles. Configure Cinemachine camera and small optional impact shake; use ProBuilder only for scene geometry, not a mandatory runtime dependency.
- [ ] Build Canvas HUD with title, boss health, score, SYNC, HULL, cooldown, parry feedback and device states, plus Ready/Pause/Win/Loss panels. No OnGUI. Default statuses are disconnected until data arrives.
- [ ] Show a skeleton only when actual joint coordinates exist; the v1 gesture-only payload must show pose text/icon rather than invented live tracking.
- [ ] Add quality/settings toggles and verify toggles change presentation without altering spawn patterns, collisions or score under a fixed simulation seed.
- [ ] Capture live gameplay and review at 1080p: ship, boss, incoming threats and safe lanes remain readable; HUD is not dithered. Save preview within the new project documentation area, not the old game.

### Task 4: Separate MQTT/HTTP bridge and simulator

**Files:** CyberDiveBridge package, stores, server, tests, simulator, .env.example and protocol.

**Interfaces:**
- `createPoseStore(nowMs)` produces `accept(payload): boolean`, `snapshot()`; tests inject monotonic clock.
- Snapshot `{valid, revision, sessionId, seq, gesture, active, confidence, ageMs}`. Server assigns revision increasing over its lifetime. Invalid snapshots force `active=false` and `gesture=NONE`; ageMs=null before any packet.
- GET `/pose/latest` returns JSON snapshot, Cache-Control no-store. GET `/controller/latest` returns `{valid, direction, left, right, ageMs}` with mode selection remaining in Unity. HTTP binds loopback.
- Broker auth uses `catchlab` username plus private configured password, requires LAN bind address; reads new `.env.local`, never imports or prints existing credentials. MQTT client IDs for simulator, Pi and ESP32 must differ.

- [ ] Write failing tests for malformed/oversized JSON, unsupported version, invalid gesture, non-finite/out-of-range confidence, negative/non-integer seq, empty session, retained messages, duplicates and reversed seq; accept only v=1, gesture NONE/PARRY, confidence in [0,1], active boolean, session length 1–64 and packet <=1024 bytes.
- [ ] Write failing tests: a valid packet is fresh through 500 ms and invalid above it; duplicate packets do not refresh age; new session resets seq and accepted snapshot; controller CSV rejects invalid values and becomes invalid after 200 ms.
- [ ] Implement stores and HTTP server. Use Aedes version compatible with installed Node, distinct from old running broker. Add request body rejection, correct 404/405 and JSON response content type.
- [ ] Write real local integration tests: start on ephemeral localhost ports, authenticate MQTT, publish a valid pose, fetch the HTTP snapshot, disconnect and verify expiry. Unauthorized MQTT must not become a valid snapshot. Integration tests must not touch port 1883 or existing service.
- [ ] Run `node --test` from CyberDiveBridge; expected all tests pass and no listeners left open.
- [ ] Add Python simulator using an environment-configured password, no hardcoded secret. It sends fresh NONE/PARRY at 10 Hz with a new UUID session per run, incrementing sequence, qos=0, retain=false. Document restart, reconnect and sending NONE when tracking is lost. Do not require camera software for the simulator.

### Task 5: Unity device receiver and two-board behavior

**Files:** BridgeClient, PilotInput integration, HUD status, receiver tests, protocol guide.

**Interfaces:** `BridgeClient.Direction(mode): float`, `TryConsumeParry(): bool`, `ControllerFresh`, `PoseFresh`, `Gesture`; receiver obtains state snapshots from Task 4, PoseGate from Task 1. Poll at bounded approximately 10 Hz with one request in flight per endpoint, timeout one second; invalidate local state before the next poll if last successful sample ages past limits.

- [ ] Write failing receiver/gate tests for active-only startup, stale/no data, reset sequence in new session, duplicate HTTP snapshot, HTTP failures, pause/resume and restart. Timestamp local receipt only for genuinely new server revision; do not refresh pose age from repeated GETs.
- [ ] Implement state consumers and polling using unscaled monotonic time; adjust local freshness by server ageMs. On network failure invalidate and disarm. Accept only validated finite schema values.
- [ ] Route keyboard and device parry intents to a single game gate so one frame cannot double-fire. Consume intents during cooldown without queuing; after pause or reconnect require neutral. Keyboard remains usable without hardware.
- [ ] Add FSR/Joystick mode selection. Test simultaneous FSR directions stop and unselected sensor data cannot move ship.
- [ ] Run Unity rule and receiver tests, Node suite, and simulator-to-Unity smoke test. Expected a simulated fresh neutral/active pair opens one shield, held active does not repeat, and stopping the simulator marks pose disconnected and prevents skills.
- [ ] With physical devices available, run ESP32 movement and Pi pose concurrently on the same Wi-Fi. Record observed latency and choose gesture hold/confidence thresholds with the friend. If hardware is unavailable, mark this exact check pending; do not claim it passed.

### Task 6: Windows build, play verification and handoff

**Files:** BuildPlayer, README_TH, PROTOCOL_TH, build output under CyberDive/Build/Windows, validation ledger.

**Interfaces:** `BuildPlayer.Build()` generates StandaloneWindows64 executable using the saved Dive scene and fails the command when BuildReport is not Succeeded.

- [ ] Build with the installed Editor and read BuildReport/exit code. Expected compile and player build succeed; import/save all assets so source files and Unity metadata travel together.
- [ ] Launch player and verify simulation actually advances over multiple frames, not just a static screenshot. Exercise move/fire/parry/pause/restart and both outcomes. Inspect console/log for runtime exceptions.
- [ ] Measure frame times during the busiest boss segment at 1080p, default and reduced effects. Report machine/settings and measurements; do not infer 60 fps from an empty scene.
- [ ] Write Thai opening/playing/building instructions and a friend-ready protocol example. Explain port conflict with the old bridge, private config setup, keyboard defaults, unavailable hardware checks and prototype scope.
- [ ] Run final Unity tests and full bridge suite, capture the player, review the changes independently against spec. Correct critical or important findings with regression tests and record deferred polish.
- [ ] Handoff executable, preview and source links with what was tested and what still needs real hardware. Subsequent work extends from the verified slice to the full two-minute mission.

## Self-review

Spec coverage: the slice deliberately validates one enemy, one boss, one parry and both-board interfaces before full-length content. New art/audio are procedural originals. Input ageing, held gestures, session reset, pause and safe fallback have explicit owning tests. Graphics quality must be measured independently of gameplay correctness. Cloud results and final soundtrack remain outside the slice. No design decisions require another preference question before starting this plan.

## Execution recommendation

Native execution by the current Sol 6.1 agent, using executing-plans, is recommended: combat, renderer and input integration share interfaces and benefit from one continuous implementation context. Use an independent final reviewer as required by that skill. Review this concrete plan before implementation per writing-plans handoff.

Environment discovery: Editor 6000.5.2f1 and CLI 1.0.0-beta.13 exist. The CLI update check reports 1.0.0 available. Initial sandboxed status cannot see Pipeline-enabled instances and explicitly warns that running editors may be hidden. Verify the new project's Editor connection with appropriate permissions before editing scenes; no inference about whether CatchLab is currently open.
