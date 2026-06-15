# True Gate? Balance v1 Implementation Plan

Version: `balance-v1.0.0`  
Audit basis: `Docs/Balance/Balance_Code_Audit.md`  
Source brief: `G:/downlaod/TrueGate_Balance_Implementation_Plan.md`

## Goal

Replace linear Projectile and Squad power multiplication with controlled scaling while preserving:

- Large squads.
- Many visible projectiles.
- Increasing enemy density.
- Fast three-lane gate decisions.
- Existing UI, save data, and scene flow.

Target full-meta power:

- Effective DPS: approximately `7–8x` base.
- Total theoretical durability: approximately `7–8x` base.
- Median full-meta run: `7–10` minutes.
- Runs above 15 minutes: below `5%`, excluding a future endless mode.

## Delivery Rules

1. Do not overwrite legacy assets before v1 assets compile and load.
2. Do not modify art assets.
3. Preserve existing public UI and save APIs where practical.
4. Change scene and prefab references only through Unity MCP.
5. Verify compilation after every code phase.
6. Add tests before tuning values from play feel.
7. Never adjust more than two balance groups in one tuning pass.

## Phase 1: Code Audit

Status: completed on 2026-06-14.

Deliverables:

- `Docs/Balance/Balance_Code_Audit.md`
- This repository-specific implementation plan.

No gameplay, scene, prefab, or ScriptableObject values are changed in this phase.

## Phase 2: Versioned Config And Math

Status: completed on 2026-06-14.

Implemented in this phase:

- Versioned ScriptableObject config classes and bootstrap references.
- `BalanceV1Math` with config-aware fallbacks.
- EditMode coverage for formulas, target full-meta ratios, and config validation.
- No balance assets were created or wired into gameplay.

Create:

```text
Assets/_Project/Scripts/Systems/Balance/BalanceV1Math.cs
Assets/_Project/Scripts/Data/Balance/CombatScalingConfig.cs
Assets/_Project/Scripts/Data/Balance/PlayerMetaBalanceConfig.cs
Assets/_Project/Scripts/Data/Balance/GatePoolConfig.cs
Assets/_Project/Scripts/Data/Balance/RunPressureConfig.cs
Assets/_Project/Scripts/Data/Balance/EnemyRoleConfig.cs
Assets/_Project/Scripts/Data/Balance/EconomyConfig.cs
Assets/_Project/Scripts/Data/Balance/BalanceTelemetryConfig.cs
Assets/_Project/Scripts/Data/Balance/BalanceBootstrapConfig.cs
```

Initial combat values:

```text
fireSoftCapStart = 6
fireSoftCapRange = 12
baseProjectileCount = 5
projectileCoverageCoefficient = 0.15
squadCoverageCoefficient = 0.45
followerHpRatio = 0.25
recruitSpawnHpRatio = 0.50
```

Math:

```text
EffectiveFireRate(F):
  F <= 6 -> F
  F > 6  -> 6 + 12(F - 6) / (12 + F - 6)

ProjectileFactor(P):
  1 + 0.15 * (sqrt(max(1, P)) - sqrt(5))

SquadFactor(S):
  1 + 0.45 * sqrt(max(0, S - 1))

FollowerDamageScale(S):
  S <= 1 -> 0
  otherwise -> (SquadFactor(S) - 1) / (S - 1)

MainBulletDamage:
  Damage * 5 * ProjectileFactor(P) / P
```

Create v1 assets only after scripts compile:

```text
Assets/_Project/Data/Balance/V1/CombatScalingConfig_v1.asset
Assets/_Project/Data/Balance/V1/PlayerMetaBalanceConfig_v1.asset
Assets/_Project/Data/Balance/V1/GatePoolConfig_v1.asset
Assets/_Project/Data/Balance/V1/RunPressureConfig_v1.asset
Assets/_Project/Data/Balance/V1/EconomyConfig_v1.asset
Assets/_Project/Data/Balance/V1/BalanceTelemetryConfig_v1.asset
Assets/_Project/Data/Balance/BalanceBootstrapConfig.asset
```

Acceptance:

- Pure math tests pass.
- Missing config falls back safely to v1 defaults.
- No existing scene reference is changed yet.

## Phase 3: Combat And Squad Integration

Status: completed on 2026-06-14.

Implemented:

- Effective Fire Rate is used for actual shot cooldown while raw Fire Rate remains visible.
- Projectile volleys normalize damage through `ProjectileFactor`.
- Followers use diminishing damage scale and 25% main Max HP.
- Gate recruits start at 50% follower HP.
- Promotion preserves follower remaining HP without replacing main Max HP.
- Permanent upgrades use the explicit v1 level table and costs.
- Existing UI and save-facing service APIs remain intact.

Modify:

- `BulletSpawner`
- `PlayerUnit`
- `PlayerController`
- `PlayerMetaUpgradeService`
- `GameManager` only if bootstrap injection is required

Implementation:

1. Keep raw Damage and Fire Rate available for UI and gates.
2. Use effective Fire Rate only for the actual shot interval.
3. Normalize volley damage by Projectile Count.
4. Apply follower damage scale based on current squad size.
5. Set follower Max HP to `main Max HP * followerHpRatio`.
6. New permanent-meta followers may start full at run start.
7. Gate-recruited followers start at `recruitSpawnHpRatio`.
8. Preserve the existing promotion behavior and remaining HP.
9. Recalculate all follower scales whenever Squad, Damage, Fire Rate, HP, or Projectile changes.

Permanent meta table:

| Level | Damage | Fire Rate | HP | Projectile | Squad | Cost |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 1.00 | 4.0 | 10.0 | 5 | 1 | - |
| 1 | 1.10 | 4.4 | 11.5 | 6 | 2 | 100 |
| 2 | 1.20 | 4.8 | 13.0 | 8 | 3 | 250 |
| 3 | 1.30 | 5.2 | 15.0 | 10 | 5 | 550 |
| 4 | 1.42 | 5.8 | 17.5 | 13 | 8 | 1100 |
| 5 | 1.55 | 6.4 | 20.0 | 16 | 12 | 2200 |

Compatibility:

- Keep `PlayerMetaUpgradeService` as the UI-facing facade.
- Upgrade ownership levels remain unchanged in existing saves.
- Do not alter UpgradePanel bindings.

Acceptance:

- Base build remains 20 theoretical DPS.
- Projectile Count raises coverage but per-bullet damage falls.
- Followers visibly fire but do not clone full DPS.
- Full meta computes near the target power range.
- Promotion never restores full HP.

## Phase 4: Enemy Pressure And Roles

Status: completed on 2026-06-14.

Implemented:

- The v1 pressure curve now controls active cap, minimum visible, threat budget,
  spawn rate, and enemy runtime scaling.
- Basic enemies fill the visual density floor without consuming special-enemy
  threat budget.
- Chomboom unlocks at 30 seconds with threat cost 1.5.
- Vomfy unlocks at 90 seconds with threat cost 2.
- Future Swarmer, Tanker, and Elite role defaults are available without
  assuming their prefabs are already wired.
- Existing Basic, Chomboom, and Vomfy scene entries continue to infer their
  roles from prefab components.
- Chomboom all-squad AoE and threat-budget behavior are covered by EditMode
  tests.

Extend progression roles:

```text
Basic
Chomboom
Vomfy
Swarmer
Tanker
Elite
```

Pressure nodes:

| Time | Active cap | Minimum visible | Threat budget | Spawn/sec | HP | Damage | Speed |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 12 | 8 | 2 | 3 | 1.00 | 0.75 | 1.00 |
| 60 | 18 | 12 | 4 | 4 | 1.15 | 0.85 | 1.05 |
| 180 | 28 | 20 | 7 | 6 | 1.50 | 1.00 | 1.12 |
| 300 | 38 | 27 | 10 | 8 | 2.10 | 1.20 | 1.18 |
| 420 | 48 | 34 | 13 | 10 | 2.90 | 1.45 | 1.24 |
| 720 | 60 | 42 | 16 | 12 | 4.50 | 1.90 | 1.32 |

Threat costs:

| Role | Threat |
|---|---:|
| Basic | 0 |
| Swarmer | 0.25 |
| Chomboom | 1.5 |
| Vomfy | 2 |
| Tanker | 3 |
| Elite | 6–10 |

Rules:

- Enforce `minimumVisible <= activeCap` in config validation and runtime.
- Keep Basic/Swarmer density separate from special-enemy pressure.
- Do not assume archetype assets are wired to prefabs; verify each prefab through Unity MCP.
- Size pool definitions for each target cap before performance tests.

Chomboom:

- Keep the existing all-unit AoE behavior.
- Add tests proving multiple squad units are hit once.
- Optimize global searches only after behavior is covered.

Acceptance:

- Active cap and minimum visible no longer conflict.
- Threat actors cannot exceed the current budget.
- Unlock times match config.
- No missing prefab or pool reference warnings.

## Phase 5: Gate Categories And Cadence

Status: completed on 2026-06-14.

Implemented:

- Gate sets use a data-backed `GatePoolConfig_v1.asset`.
- Runtime cadence is 15 seconds with Stable, Utility, and Risky lane roles.
- Major eligibility is evaluated every 60 seconds with phase-based chance.
- The main pool no longer generates legacy `x2` or `/2` operations.
- Stable, Utility, Risky, and Major entries support composite effects.
- Barrier, healing, enemy speed, enemy pressure, incoming damage, and Bounty
  timed modifiers are run-scoped.
- Timed modifiers use scaled game time, remain frozen during pause, and restore
  their affected systems when they expire.
- Existing gate prefab movement, trigger, lane sizing, and pooling remain
  unchanged.

Preserve existing gate lane layout, movement, trigger, and pooling.

Replace runtime operation generation with data entries:

```text
Stable
Utility
Risky
Major
```

Cadence:

- Gate set every `15` seconds.
- Three lanes: Stable, Utility, Risky.
- Major eligibility every `60` seconds.
- Major may replace Stable or Risky according to phase weight.

Initial entries:

| Category | Effect |
|---|---|
| Stable | +10% Damage |
| Stable | +0.20 raw Fire Rate |
| Stable | +8% Max HP and heal the gained amount |
| Utility | Heal 20% missing HP |
| Utility | One-hit barrier for 15 seconds |
| Utility | Enemy speed -25% for 20 seconds |
| Major | +1 Projectile |
| Major | +1 Squad at 50% follower HP |
| Major | +15% Damage and +8% Fire Rate |
| Risky | +25% Damage, +20% incoming damage |
| Risky | +1 Projectile, -12% Damage |
| Risky | +1 Squad, +15% enemy pressure |
| Risky | +50% coin reward for 30 seconds, +15% enemy speed |

Deferred from v1 unless supporting systems already exist:

- Magnet
- Projectile cleanse
- Reroute

These effects currently have no pickup or reroll infrastructure. Deferring them keeps Phase 5 focused and testable.

Acceptance:

- Main pool contains no `x2` or `/2`.
- Each set follows category rules.
- Major cadence is deterministic and testable.
- Timed drawbacks expire correctly and survive pause/resume.

## Phase 6: Economy, Score, And Save Migration

Status: completed on 2026-06-15.

Change enemy progression reward to a float reward point:

| Role | Reward |
|---|---:|
| Basic | 0.20 |
| Swarmer | 0.10 |
| Chomboom | 0.75 |
| Vomfy | 1.00 |
| Tanker | 2.00 |
| Elite | 12–18 |

Run accounting:

```text
coinRewardPoints += enemyReward
timeScore = floor(survivalSeconds * 0.5)
finalScore = killScore + timeScore + eliteBonus
finalCoins = round(coinRewardPoints + timeBonus + milestoneBonus)
```

Rules:

- Keep fractional reward points internal to the run.
- Show rounded run coins in existing UI.
- Commit wallet coins only from `EndRun`.
- Do not grant run coins on scene exit before a completed defeat flow unless explicitly designed later.

Save migration:

- Add `balanceVersionLastPlayed`.
- Increment schema only for serialized changes.
- Preserve wallet, best records, and upgrade ownership.
- Continue supporting PlayerPrefs-to-JSON migration.

Acceptance:

- Enemy density changes do not directly multiply wallet income.
- Score and coin are independent.
- Existing saves load without reset.
- Upgrade purchases still use integer wallet coins.

## Phase 7: Local Balance Telemetry

Status: completed on 2026-06-15.

Create:

```text
Assets/_Project/Scripts/Systems/Telemetry/BalanceTelemetryService.cs
```

Output only in Editor or development builds by default:

```text
run_summary.csv
run_snapshot_15s.csv
gate_events.jsonl
```

Events:

- run_start
- snapshot_15s
- gate_shown
- gate_selected
- first_hit
- follower_death
- promotion
- run_end

Performance constraints:

- No per-frame file writes.
- Buffer events and flush at run end or safe checkpoints.
- Cap snapshots per run.
- Include build and balance config versions.

Acceptance:

- A completed run produces one summary row.
- Snapshot interval is configurable.
- Telemetry failure never blocks gameplay or save.

## Phase 8: Tests And Simulator

Status: completed on 2026-06-15.

EditMode tests:

- Effective Fire Rate base and soft-cap behavior.
- Projectile Factor at base count.
- Squad Factor and follower scale.
- Damage per bullet decreases as Projectile Count rises.
- Effective DPS remains monotonic.
- Config validation clamps invalid values.

PlayMode tests:

- Permanent upgrades apply exactly once per run start.
- Follower HP ratio.
- Recruit HP ratio.
- Promotion preserves remaining HP.
- Chomboom hits all nearby units once.
- Pressure node always satisfies minimum visible <= cap.
- Threat budget blocks special enemy spawn.
- Gate and Major cadence.
- Wallet commit occurs at run end.

Simulator:

```text
Tools/Balance/balance_simulator_true_gate_v1.py
Tools/Balance/output/
```

The simulator should consume exported config data rather than duplicate hidden constants.

## Phase 9: Unity Wiring And Verification

Status: completed locally on 2026-06-15. Real-device execution remains pending
because no Android device was connected through ADB.

Implemented:

- Created the complete versioned v1 asset set under
  `Assets/_Project/Data/Balance/V1`.
- Assigned `BalanceBootstrapConfig_v1` to `GameManager` as the runtime owner.
- Wired combat scaling to `PlayerController` and the active `BulletSpawner`.
- Wired `RunPressureConfig_v1` and explicit Basic, Chomboom, and Vomfy role
  assets to the active enemy spawn entries.
- Kept Swarmer, Tanker, and Elite role assets ready without spawning them
  before matching prefabs exist.
- Confirmed existing pool sizes cover the v1 pressure caps for currently
  available enemy roles.
- Re-exported simulator input from the wired ScriptableObject assets.

Verification:

- Compilation: 0 errors and 4 known non-blocking warnings.
- Missing scene scripts/references: 0.
- EditMode tests: 38 passed, 0 failed.
- PlayMode tests: 5 passed, 0 failed.
- Thirty-second Play Mode check: working set increased by about 6 MB, log grew
  by 2.7 KB, and no runtime exception was recorded.
- Device Simulator: verified with `Punch Hole Center (1440x3088)`.
- Ten-minute wall-clock soak: 21 samples, all responsive, working set moved
  from about 2.59 GB to 2.81 GB as pressure increased, and log growth was
  29.8 KB with no exception.
- Android IL2CPP Development build: succeeded with 0 errors and 5 non-blocking
  warnings. APK size is 122.46 MB.
- Real-device install/run: pending because `adb devices` returned no device.
- The local player save was backed up and restored after runtime checks.

Detailed evidence is recorded in
`Docs/Balance/Phase9_Unity_Wiring_Verification.md`.

Use AB Unity MCP to:

1. Confirm the correct Unity instance.
2. Compile scripts.
3. Create versioned ScriptableObject assets.
4. Assign `BalanceBootstrapConfig` to the runtime owner.
5. Wire enemy role prefabs and pool definitions.
6. Replace the active run progression reference with v1.
7. Replace gate pool reference with v1.
8. Save `Assets/_Project/Scenes/Main.unity`.

Verification order:

1. EditMode tests.
2. Play Mode in normal Game view.
3. Thirty-second memory and log check.
4. Device Simulator portrait view.
5. Ten-minute soak run.
6. Android development build on a real device.

## Rollback

Keep legacy assets untouched:

```text
Assets/_Project/Data/Player/MainPlayerUnitConfig.asset
Assets/_Project/Data/Spawning/DefaultRunProgressionConfig.asset
Assets/_Project/Data/Gates/*.asset
```

Rollback should require changing the active bootstrap or scene references, not reverting code by hand. Compatibility fallbacks must allow the scene to start when the v1 bootstrap is absent.

## Completion Criteria

- No-meta runs are no longer effectively immortal at 3:30.
- Full-meta players can still die.
- Full-meta theoretical DPS and durability remain near target.
- Projectile Count is coverage-oriented.
- Squad followers have reduced damage and HP.
- Chomboom damages clustered squad members.
- Gate main pool has no `x2` or `/2`.
- Spawn cap and minimum-visible values are consistent.
- Coin income is based on fractional reward points.
- Save migration preserves progression.
- Local telemetry exports successfully.
- Math and critical run-flow tests pass.
- At least one documented playtest and one Android performance pass exist.
