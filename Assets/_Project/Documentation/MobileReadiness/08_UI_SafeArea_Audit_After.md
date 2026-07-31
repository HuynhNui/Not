# UI Safe Area Audit - After

## Build Scope

- Enabled build scene: `Assets/_Project/Scenes/Main.unity`
- Target reference resolution: portrait Android, approximately 1080 x 2400
- Full-screen backgrounds remain outside panel-level fitters so they continue to cover the display.
- Package and third-party demo scenes are not included in the player build and are excluded from the release gate.

| Surface | Safe-area structure | Runtime validation | Result |
|---|---|---|---|
| Main menu and shared UI root | `GameCanvas/UIRoot/SafeAreaRoot` | Hierarchy test plus portrait Game View smoke | Pass |
| Gameplay HUD | Shared safe root, full-screen gameplay remains edge-to-edge | Hierarchy test and pause smoke | Pass |
| Pause | `PausePanel/PauseSafeAreaRoot` | Portrait capture; all commands and toggles inside bounds | Pass |
| Settings | `SettingsPanel/SettingsSafeAreaRoot` | Portrait capture; rows, toggles, back, and reset inside bounds | Pass |
| Mission log | `MissionLogPanel/MissionSafeAreaRoot/PanelCard` | Portrait capture; header and scroll viewport inside bounds | Pass |
| Game over | Existing fitted content frame under shared root | Hierarchy test | Pass |
| Upgrade | Shared safe root; content uses its existing world-space presentation | Hierarchy test | Pass |
| Tutorial overlay | Shared safe root; skip control anchored to the lower safe corner | Hierarchy test | Pass |
| Story cutscene | Runtime cutscene canvas uses its own fitted content root | Runtime smoke | Pass |

## Automated Coverage

- Seven EditMode safe-area tests passed.
- `SafeAreaHierarchyPlayModeTests` passed in the full PlayMode suite.
- The test verifies one fitter per required content hierarchy and rejects nested fitters that would double-apply insets.
- Safe-area math handles zero-size startup frames, orientation changes, padding, and repeated application without layout drift.

## Visual Evidence

- `Temp/MobileReadiness/Settings_Final.png`
- `Temp/MobileReadiness/Mission_Final.png`
- `Temp/MobileReadiness/Pause_Final.png`

The captures are local validation artifacts and are intentionally not player assets.
