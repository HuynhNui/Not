# True Gate Beta 0.1.0 Mobile Readiness Summary

## Release Decision

**Ready for direct-install Android beta testing.**

The RC builds, installs, launches, preserves save data, and completes the tested menu/gameplay/settings flows on the connected OPPO device without crash or ANR. A private signing keystore remains mandatory before store/public production distribution.

## Completed Work

- Applied safe-area fitting to the build UI, including Settings, Pause, Mission Log, tutorial, and runtime cutscene content.
- Verified portrait layouts in Game View and at 1080 x 2400 on a physical Android device.
- Converted mobile audio policy from 221 to 11 `DecompressOnLoad` clips and from 229 to 21 preloaded clips.
- Reduced estimated texture runtime memory from 891,336,804 to 327,483,116 bytes (about 63.3%).
- Added 9 ETC2 sprite atlases covering 75 build texture sources, with zero duplicate sources and zero large backgrounds packed.
- Configured the Android player as IL2CPP ARM64, API 25 minimum, portrait-only, ETC2, version `0.1.0 (1)`.
- Removed the startup AudioEventRouter fallback warning by wiring its PlayerController reference.
- Fixed repeated enemy contact so trigger and overlap paths enforce the same damage cooldown.
- Fixed final-choice progression so `TERMINAL PROTOCOL` unlocks only after the final choice and starts fresh category chains within the Terminal phase.
- Built and verified development and release-candidate APKs, then installed both with save-preserving upgrades.

## Evidence

- `05_Audio_Audit_After.csv`
- `06_Texture_Audit_After.csv`
- `07_SpriteAtlas_Audit_After.md`
- `08_UI_SafeArea_Audit_After.md`
- `09_Build_Verification.md`
- Device captures under `Temp/MobileReadiness/Device/`

## Known Baseline Test Debt

EditMode failures:

1. `BalanceV1MathTests.GateLogic_IgnoresDeadFollower` uses `Destroy` during EditMode.
2. `GateScalingProfileTests.EliteSquad_MathMatchesLinearSquadAndVisualBudget` expects 344.595 but runtime math returns about 229.935.

PlayMode failures:

1. Three tests create `BulletSpawner` fixtures without a FirePoint reference.
2. Three projectile-meta tests expect projectile counts above the runtime cap/value (expected 4 or 6, actual 3).

The final-choice mission failure is now fixed, with all 15 `MissionRuntimePhaseETests` passing. The remaining failures predate the fix; current suites are 211/213 EditMode and 35/41 PlayMode. Full results are recorded in `Assets/_Project/Documentation/QA/2026-07-31_Test_Run.md`.

## Distribution Follow-up

- Create and securely store a private Android release keystore.
- Rebuild/sign the same RC configuration with that key for Play Store or public distribution.
- Run a longer performance soak with a frame-time capture tool if a formal 30/60 FPS certification is required.
