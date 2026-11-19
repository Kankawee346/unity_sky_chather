# Visual Overhaul Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans; one independent final review.

**Goal:** Replace debug-like presentation with readable arcade HUD/menu and visible, coherent energy effects.
**Architecture:** Keep runtime-generated uGUI and pooled combat. Add reusable arc graphic and presentation component for owned post-processing/exhaust; keep gameplay in DiveGame.
**Tech:** Unity6000.5 URP17.5, uGUI, InputSystem, existing vector shaders.
**Spec:** docs/superpowers/specs/2026-11-15-visual-overhaul-design.md

## Constraints
No combat balance changes. Retain options/global best/input opt-in, all previous builds, reduced-effects and flash/shake preferences. Existing standalone workspace is not a Git checkout.

## Review focus
Menu Enter ownership; negative/zero cooldown values; HUD after damage/pause/retry; transient FX cleared on stop/restart; shader inclusion/native build and reduced FX.

- [ ] Task1 HUD/menu: add functional regression for hull/cooldown/hidden telemetry, observe RED; replace TerminalHud layout and add HudRing; run PlayMode incl existing options/Enter tests; visually inspect actual menu and HUD.
- [ ] Task2 effects: meaningful regression for reduced-FX parry cue cleanup; pooled rings/streaks, additive vertex-color shader, projectile trails, cyan shield and exhaust, selective bloom. Existing pooling/expiry/combat tests remain green; check actual screenshots and shader output.
- [ ] Task3 final: rule+PlayMode suites, independent Astra review, full normal flight capture and local effect capture, versioned Windows build/native smoke, evidence and README.

Progress: Current UI is flat text with seven lines of always-on telemetry, tiny meter strokes and equally weighted buttons. Effects are ten shrinking .07-unit cubes; a stock orange VFX Graph burst conflicts with the palette. These are the specific targets, not more gameplay features.
