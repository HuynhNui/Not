# Phase 9 Unity Wiring And Verification

Date: 2026-06-15

## Wiring

- Runtime owner: `GameManager`
- Bootstrap: `BalanceBootstrapConfig_v1`
- Active balance version: `balance-v1.0.0`
- Scene: `Assets/_Project/Scenes/Main.unity`
- Scene missing scripts/references: 0
- Active enemy roles: Basic, Chomboom, Vomfy
- Future role assets created but not spawned: Swarmer, Tanker, Elite

## Automated Tests

| Suite | Passed | Failed |
|---|---:|---:|
| EditMode | 38 | 0 |
| PlayMode | 5 | 0 |

Compilation completed with 0 errors and 4 known non-blocking warnings: two
third-party obsolete API warnings and two unused legacy serialized fields.

## Runtime Checks

Thirty-second check:

- Working set: about 2777.5 MB to 2783.4 MB
- Editor log growth: 2.7 KB
- Runtime exceptions: 0

Ten-minute wall-clock soak:

- Samples: 21 at 30-second intervals
- Working set: about 2590.5 MB to 2812.7 MB
- Editor responsive samples: 21/21
- Editor log growth: 29.8 KB
- Runtime exceptions: 0
- Late sample: 46 active enemies, 43 visible, cap 53, threat 14 of 14.29

The soak player received temporary in-memory HP only for performance isolation.
No asset or save value was changed.

## Device Simulator

- Device: `Punch Hole Center (1440x3088)`
- Portrait layout and safe-area placement remained coherent.
- The Simulator editor window required scrolling because it was shorter than
  the emulated device; this did not indicate runtime clipping.

## Android Build

- Target: Android IL2CPP Development
- Result: succeeded
- Errors: 0
- Warnings: 5
- Build time: 736.29 seconds
- APK: `Builds/Android/TrueGate_Phase9_Dev.apk`
- APK size: 122.46 MB
- SHA-256:
  `DFF2261571354466D287037D3BCEC9A4A7DDE8C0D6770345B721451DB4CBFDD4`

Warnings:

- Development diagnostics recommend fuller debug symbols.
- Two obsolete `GetInstanceID` warnings come from the third-party
  `2D Health & Damage System` asset.
- Two serialized legacy fields are currently unused:
  `GateSystem.spawnIntervalSeconds` and
  `EnemySpawnerSystem.minimumVisibleEnemies`.

## Pending

No Android device was connected. `adb devices -l` returned an empty device
list, so install, launch, and on-device profiling remain pending.

## Save Safety

Runtime verification initially completed one test run. The pre-test
`save.json` and `save.bak` were restored afterward:

- schema version: 1
- revision: 35
- wallet coins: 1938
