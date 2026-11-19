# Player options iteration

User requested continuing the keyboard-first game. Add a focused replayability/usability pass, preserving camera opt-in and current campaign.

- [x] Difficulty Easy/Normal/Hard changes hostile projectile/drone speed and cadence only; Normal retains existing values. Difficulty chosen before run, locked during playing/pause, snapshotted at BeginRun.
- [x] Persist difficulty and FX/shake/flash/mute choices; keep keyboard default on each launch. Existing best score remains global and HUD labels BEST ALL, no data migration/deletion.
- [x] Add visible menu difficulty/audio buttons, M mute, existing F1/F2/F3 persist; show selected/current difficulty in HUD.
- [x] Regression tests, independent Astra review, live preview, versioned Windows Build/Options; old builds preserved.

Speed factors .8/1/1.2; regular volley periods8/6/4 beats, enraged6/4/3. Keep player movement, damage, parry window, timer and boss HP unchanged. No claim of full per-difficulty balancing before user play feedback.

RED evidence: EditMode 8/9 passed (speed factors still stubbed); PlayMode 7/8 passed (missing TrySelectDifficulty). Implemented difficulty snapshots, hostile speed/cadence scaling, option persistence and menu controls next. Work remains in existing standalone CyberDive project; parent workspace is not a Git checkout, so no commits/worktree operations apply.

Initial GREEN: EditMode 9/9, PlayMode 8/8. Independent Astra review found one Important issue: Enter also submits a previously selected menu option. Added actual InputSystem keyboard + pointer regression: PlayMode 8/9 RED (expected muted=true, actual=false). Fixed by disabling EventSystem navigation events: DiveGame owns keyboard commands, pointer still controls menus. Filled review's minor test gaps: all six volley periods and actual hostile/friendly velocities now asserted. No deferred findings from this iteration.

Final GREEN after review fix: EditMode 9/9 and PlayMode 9/9; reports options-rules.xml and options-play.xml. Actual menu pointer click changed Normal to Hard in Editor. Test-only selection restored to Normal and audio on before full-flight verification.

Live verification: keyboard-input driver completed Normal with Won at 93.61066s, HULL 3, score21600, parries27. Screenshot Screenshots/cyber-dive-options-1080p.png; current Editor consoleErrors=0 (historical pipeline log stream contains earlier TestRunner errors, not this run). Windows Build/Options succeeded 2026-11-15T09:26:43Z. Launched native executable, clicked mute/unmute and Begin, observed mission progress from0.1s to5.1s with projectiles and drones. Native injected Return/Escape were not observed by the game through CUA, so keyboard behavior is evidenced by InputSystem tests/full-flight driver rather than claimed as manually verified in native player. Audio restored ON, Normal selected. Old builds retained. Easy/Hard balance still needs human play feedback as scoped.
