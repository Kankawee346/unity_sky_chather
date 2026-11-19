# CYBER//DIVE visual overhaul

User says current effects and UI do not look good. Prior instruction delegates visual decisions and says to proceed. Keep Unity, 2.5D, black/white/cyan identity and current combat rules. This is a presentation pass, not new game mechanics.

Approaches considered: small typography touch-up (insufficient change for this feedback); textured neon reskin (loses the existing vector identity); cohesive vector arcade presentation (chosen by implementer under delegated design authority). Build the third: strong hierarchy, legible meters, animated geometric art and energy effects with restrained selective glow.

HUD: score and mode upper left, mission/boss progress above center, countdown upper right. Three hull pips lower left; large parry cooldown ring lower center; SYNC/multiplier lower right. Telemetry hidden by default, F4 toggles it. Playfield remains clear. Menus get a large wordmark, readable objective, prominent primary action, organized difficulty/device/settings buttons, holographic ring motif and subtle entrance animation. Existing difficulty lock, option persistence and pointer-only menu submission remain.

FX: replace tiny cube debris and orange placeholder burst with pooled expanding rings, tapered energy streaks and local parry burst; projectile trails and engine plumes establish motion. HDR bloom applies to energy, not all white geometry. Reduced FX disables energy extras, optional full-screen flash remains off by default, shake toggle respected. No full-screen strobe or motion blur.

Validation: existing rules/combat/options regression suite, functional HUD-state test, selective-glow/low-FX behavior, independent review, real full mission screenshot plus effect close-up and menu inspection, versioned Build/Presentation. Existing outputs retained. Aesthetics need the user's visual judgment; test success alone is not evidence of visual quality.

User supplied a concrete monochrome GAME OVER reference while implementation was active: large pixel block title with offset outline layers, nested thin frames and subtle scanlines. Apply this steering directly: original generated 5x7 block-title mesh (no copied screenshot/branding/social overlays), layered frame shadows and menu-only scanlines. Keep in-game labels legible and the playfield free of scanlines.
