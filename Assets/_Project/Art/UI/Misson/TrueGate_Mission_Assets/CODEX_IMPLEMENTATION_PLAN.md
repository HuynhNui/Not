# TRUE GATE? — Mission System + Daily Reward Button Replacement

## Codex instruction

Implement the sequential Mission System and Mission Log UI in `HuynhNui/Not`.

The current Main Menu contains a visible **Daily Reward** button on the right side above `START RUN`. Replace that exact button with the Mission button. Do not use a locked bottom-navigation slot and do not add a second Mission entry.

Use the supplied assets under:

```text
Assets/_Project/Art/UI/MissionSystem/
```

## Phase A — Mandatory audit, no code changes

Inspect the active Main scene, Main Menu prefab/instance, and every component attached to the visible Daily Reward object.

Report:

1. Exact hierarchy path.
2. Prefab source and instance overrides.
3. Existing Button/Image/TMP/Animator/custom components.
4. Existing persistent and runtime click listeners.
5. Any real daily-reward timer, claim, save, economy, or notification logic.
6. Exact current sprite/font paths.
7. Whether it is purely decorative.

Search candidate object names/text:

```text
DailyReward
DailyRewardButton
DailyRewardPanel
RewardButton
DAILY REWARD
READY
```

The current `UISystem` has no Daily Reward field, so reuse the existing visual object and do not create a duplicate.

## Replace the Daily Reward object

After audit:

- Rename it `MissionButton`.
- Preserve position, anchors, pivot, size, safe-area behavior, sorting, and layout order.
- Remove only Daily Reward-specific listeners/components.
- Keep generic Button/Image/CanvasGroup/layout/press effects.
- Change its label to `MISSION`.
- Replace the sprite with `mission_button_alert_128x160.png`.
- Replace the coin icon with `mission_icon_log_64.png`.
- Reuse the top-right badge or use `mission_badge_alert_32.png`.
- Add `mainMenuMissionButton` to `UISystem`.
- Wire it to `ShowMissionLog()`.

Button states:

```text
Unread/new mission      -> mission_button_alert_128x160.png
No unread notification  -> mission_button_normal_128x160.png
Just completed           -> mission_button_complete_128x160.png
```

Opening Mission Log clears only the unread badge.

## Sequential mission rule

Exactly one mission is active.

- Earlier: Completed.
- Current: Active.
- Next: `ENCRYPTED OBJECTIVE`.
- All later: Locked phase cards.

Completing the current mission automatically:

1. marks it complete;
2. grants reward once;
3. saves immediately;
4. unlocks exactly the next mission;
5. marks the new mission unread;
6. refreshes Mission button;
7. shows `MISSION COMPLETE`;
8. shows `NEW DIRECTIVE UNLOCKED`.

No manual Claim button.

## Full mission chain

| # | ID | Phase | Mission | Evaluation |
|---:|---|---|---|---|
| 1 | boot_finish_tutorial | BOOT | FINISH TUTORIAL | gameplayTutorialCompleted |
| 2 | boot_first_loop | BOOT | COMPLETE FIRST LOOP | totalRunsCompleted >= 1 |
| 3 | boot_purchase_upgrade | BOOT | PURCHASE ANY UPGRADE | upgrade-level delta +1 after unlock |
| 4 | boot_select_3_gates | BOOT | PASS THROUGH 3 GATES | selected-gate delta 3 |
| 5 | observe_3_loops | OBSERVE | COMPLETE 3 LOOPS | totalRunsCompleted >= 3 |
| 6 | observe_100_kills_run | OBSERVE | DEFEAT 100 ENEMIES IN ONE RUN | run kills >= 100 |
| 7 | observe_dmg_lv2 | OBSERVE | UPGRADE DMG TO LV.2 | Damage level >= 2 |
| 8 | observe_survive_120 | OBSERVE | SURVIVE 2 MINUTES | run time >= 120 |
| 9 | memory_select_10_gates | MEMORY LEAK | PASS THROUGH 10 GATES | selected-gate delta 10 |
| 10 | memory_10_loops | MEMORY LEAK | COMPLETE 10 LOOPS | totalRunsCompleted >= 10 |
| 11 | memory_survive_180 | MEMORY LEAK | SURVIVE 3 MINUTES | run time >= 180 |
| 12 | memory_three_upgrades_lv2 | MEMORY LEAK | RAISE 3 CORE UPGRADES TO LV.2 | count >= 3 |
| 13 | command_1000_total_kills | HUMAN COMMAND | DEFEAT 1,000 ENEMIES TOTAL | totalEnemyKills >= 1000 |
| 14 | command_20_loops | HUMAN COMMAND | COMPLETE 20 LOOPS | totalRunsCompleted >= 20 |
| 15 | command_survive_300 | HUMAN COMMAND | SURVIVE 5 MINUTES | run time >= 300 |
| 16 | command_squad_3 | HUMAN COMMAND | REACH SQUAD SIZE 3 | squad value >= 3 |
| 17 | fatigue_major_5 | SYSTEM FATIGUE | TRIGGER 5 MAJOR GATES | selected Major gate delta 5 |
| 18 | fatigue_35_loops | SYSTEM FATIGUE | COMPLETE 35 LOOPS | totalRunsCompleted >= 35 |
| 19 | fatigue_survive_360 | SYSTEM FATIGUE | SURVIVE 6 MINUTES | run time >= 360 |
| 20 | fatigue_max_one_upgrade | SYSTEM FATIGUE | MAX ANY CORE UPGRADE | maxed core count >= 1 |
| 21 | break_max_all_upgrades | BREAK THE CYCLE | MAX ALL 5 CORE UPGRADES | maxed core count >= 5 |
| 22 | break_3000_total_kills | BREAK THE CYCLE | DEFEAT 3,000 ENEMIES TOTAL | totalEnemyKills >= 3000 |
| 23 | break_survive_420 | BREAK THE CYCLE | SURVIVE 7 MINUTES | run time >= 420 |
| 24 | break_final_choice | BREAK THE CYCLE | MAKE THE FINAL CHOICE | either final branch resolved |

Core upgrades:

```text
Damage
FireRate
MaxHp
ProjectileCount
SquadSize
```

Ignore `MoveSpeed`.

## Runtime files

Create:

```text
Assets/_Project/Scripts/Systems/MissionSystem/
├── MissionSystem.cs
├── MissionCatalog.cs
├── MissionDefinition.cs
├── MissionObjectiveType.cs
├── MissionProgressMode.cs
├── MissionProgressEvaluator.cs
└── MissionProgressSnapshot.cs
```

Create:

```text
Assets/_Project/Data/Missions/MissionCatalog_v1.asset
Assets/_Project/Editor/MissionCatalogBuilder.cs
```

Progress modes:

```text
AbsoluteLifetime
DeltaSinceUnlock
BestSingleRun
```

## SaveData

Increment schema and add:

```csharp
public string activeMissionId;
public float activeMissionProgress;
public float activeMissionBaseline;
public List<string> completedMissionIds;
public List<string> grantedMissionRewardIds;
public int lifetimeGatesSelected;
public int lifetimeMajorGatesSelected;
public bool missionNotificationUnread;
public bool finalChoiceResolved;
```

Update CreateNew, Normalize, Clone, migration, Reset, and cloud/local replacement.

Add idempotent reward API:

```csharp
public bool GrantMissionRewardOnce(string missionId, int coinAmount)
```

## Integration

### GateSystem

Subscribe to `GateSystem.GateSelected`.

Major gate:

```csharp
config.Category == BalanceGateCategory.Major
```

Tutorial gates do not count.

### Run end

After:

```csharp
runStatsTracker.EndRun();
RunStatsSnapshot snapshot = runStatsTracker.CreateSnapshot();
```

call:

```csharp
missionSystem.EndRun(snapshot);
```

then continue telemetry, story and Game Over.

### Upgrade

Reevaluate after successful purchase / `SaveService.DataChanged`.

### Tutorial

Mission 1 completes after `MarkGameplayTutorialCompleted()`.

### Final choice

Call:

```csharp
missionSystem.NotifyFinalChoiceResolved(branchId);
```

after either final branch is successfully recorded.

### Benchmark

Benchmark runs must not progress missions or grant rewards.

## Mission Log UI

Create:

```text
Assets/_Project/Scripts/Systems/UISystem/MissionLogPanelUI.cs
Assets/_Project/Scripts/Systems/UISystem/MissionRowUI.cs
Assets/_Project/Scripts/Systems/UISystem/MissionToastUI.cs
Assets/_Project/Prefabs/UI/MissionLogPanel.prefab
Assets/_Project/Prefabs/UI/MissionRow.prefab
Assets/_Project/Editor/MissionLogPanelBuilder.cs
```

Add `Mission` to `UISystem.UIScreen`.

Add fields:

```csharp
[SerializeField] private GameObject missionPanel;
[SerializeField] private Button mainMenuMissionButton;
[SerializeField] private Button missionBackButton;
[SerializeField] private MissionLogPanelUI missionLogPanelUI;
[SerializeField] private Image mainMenuMissionButtonImage;
```

`ShowMissionLog()` must:

- set Mission screen;
- refresh the panel;
- scroll to active mission;
- mark notification read;
- update the Mission button sprite.

Back returns Main Menu.

## Asset import

```text
Texture Type: Sprite (2D and UI)
Filter Mode: Point
Compression: None
Mip Maps: Off
Alpha Is Transparency: On
Wrap Mode: Clamp
```

Panel border:

```text
mission_panel_9slice_128.png
Left 24, Right 24, Top 24, Bottom 24
```

## Required tests

1. New save activates mission 1.
2. Only one mission is active.
3. Locked missions do not progress.
4. Completing active unlocks exactly one next mission.
5. Delta baseline works.
6. Major count requires Major category.
7. Tutorial gates do not count.
8. Run-kill objective is single-run.
9. Survival objective is single-run.
10. Upgrade objective reacts after purchase.
11. Reward grants once.
12. Reload restores mission.
13. Reset returns to mission 1.
14. Old schema migrates.
15. Benchmark cannot progress.
16. Either final branch finishes mission 24.
17. Daily Reward object no longer exists visually/functionally.
18. Exactly one Mission button exists.
19. Mission button remains in old Daily Reward position.
20. Badge clears after opening Mission Log.
21. Back returns Main Menu.
22. Existing Start Run, Upgrade, Settings, Tutorial, Cutscene and Game Over still work.

## Work order

1. Audit only.
2. Import assets.
3. Pure mission data/evaluator.
4. Save migration and reward idempotency.
5. Runtime integration.
6. Replace Daily Reward object.
7. Build Mission Log.
8. Tests and regression.

Do not proceed past a failing phase. After each phase, report files changed, tests run, and Inspector wiring remaining.
