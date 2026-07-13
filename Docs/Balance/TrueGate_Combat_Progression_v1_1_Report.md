# True Gate Combat Progression v1.1 Report

Status: Implementation complete for Pass A code/config wiring; production balance pending real playtest telemetry.

## Root cause

`PlayerMetaUpgradeService.ApplyToPlayer()` applied Damage, Fire Rate, Max HP, and Squad Size from meta progression, but always forced `BulletSpawner.ProjectileCount` to `1`. This made the `BULLET` meta upgrade spendable and visible in UI while runtime combat ignored it. `GetPowerScore()` already used the meta projectile table, so UI/power score and runtime DPS diverged.

## Files changed

- `Assets/_Project/Scripts/Systems/ProgressionSystem/PlayerMetaUpgradeService.cs`
- `Assets/_Project/Scripts/Systems/Balance/BalanceV1Math.cs`
- `Assets/_Project/Scripts/Data/Balance/CombatScalingConfig.cs`
- `Assets/_Project/Scripts/Data/Balance/PlayerMetaBalanceConfig.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/BulletSpawner.cs`
- `Assets/_Project/Scripts/Systems/Telemetry/BalanceTelemetryService.cs`
- `Assets/_Project/Tests/Editor/BalanceV1MathTests.cs`
- `Assets/_Project/Tests/PlayMode/BalanceRuntimePlayModeTests.cs`
- `Assets/_Project/Data/Balance/V1_1/CombatScalingConfig_v1_1.asset`
- `Assets/_Project/Data/Balance/V1_1/PlayerMetaBalanceConfig_v1_1.asset`
- `Assets/_Project/Data/Balance/V1_1/BalanceTelemetryConfig_v1_1.asset`
- `Assets/_Project/Data/Balance/V1_1/BalanceBootstrapConfig_v1_1.asset`
- `Assets/_Project/Scenes/Main.unity`
- `Docs/Balance/TrueGate_Combat_Progression_v1_1_Report.md`

Unrelated pre-existing dirty file left untouched:

- `Assets/Front/Upheaval_TMP.asset`

## Before/after config

Combat defaults:

| Field | Before | After |
|---|---:|---:|
| config version | balance-v1.0.0 | balance-v1.1.0 |
| projectile coverage coefficient | 0.15 | 0.20 |
| squad coverage coefficient | 0.45 | 0.55 |
| follower HP ratio | 0.25 | 0.25 |
| recruit spawn HP ratio | 0.50 | 0.50 |

Meta Damage table:

| Level | Before | After |
|---:|---:|---:|
| 0 | 1.00 | 1.00 |
| 1 | 1.10 | 1.15 |
| 2 | 1.20 | 1.30 |
| 3 | 1.30 | 1.50 |
| 4 | 1.42 | 1.70 |
| 5 | 1.55 | 1.90 |

Projectile, Fire Rate, HP, Squad, and Cost tables are unchanged in code defaults.

## Before/after DPS

The v1.1 math tests now target:

| Meta level | Proposed theoretical DPS | Base ratio |
|---:|---:|---:|
| 0 | 20.0 | 1.00x |
| 5 | 231.8 | 11.59x |

Validated expected constants in tests:

- `ProjectileFactor(5) == 1.0`
- `ProjectileFactor(16, 0.20) ~= 1.3528`
- `SquadFactor(12, 0.55) ~= 2.8241`
- `FollowerDamageScale(12, 0.55) ~= 0.1658`
- `DamagePerMainBullet(1.90, 16) ~= 0.803`
- Full-meta/base ratio target: `11.0x-12.2x`

## Visual tier mapping

Inspected through Unity MCP before the bridge became unreachable:

| Existing entry | Old minDamage | Prefab |
|---:|---:|---|
| 0 | 0 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_00.prefab` |
| 1 | 10 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_10.prefab` |
| 2 | 20 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_20.prefab` |
| 3 | 50 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_50.prefab` |
| 4 | 100 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_100.prefab` |

Applied v1.1 mapping through Unity MCP:

| Entry | New minDamage |
|---:|---:|
| 0 | 1.30 |
| 1 | 1.60 |
| 2 | 1.90 |
| 3 | 2.50 |
| 4 | 3.25 |

`BulletSpawner.ResolveVisualTierIndex(float visualDamage)` was added and tested so Projectile Count changes do not lower tier; visual tier remains based on `PlayerUnit.Damage`.

## Telemetry

Snapshot rows now include:

- `effective_dps_estimate`
- `projectile_factor`
- `squad_factor`
- `follower_damage_scale`
- `main_damage_per_projectile`
- `kills_since_previous_snapshot`

Run summaries now include:

- `peak_effective_dps_estimate`
- `peak_damage`
- `peak_projectile_count`
- `peak_squad_count`
- `first_follower_death_seconds`

Output directory is versioned:

`BalanceTelemetry/<balance-version>/run_summary.csv`
`BalanceTelemetry/<balance-version>/run_snapshot_15s.csv`
`BalanceTelemetry/<balance-version>/gate_events.jsonl`

Telemetry write failures remain caught and logged without throwing into gameplay.

## Unity wiring

Applied through Unity MCP:

- `PoolSystem/GameManager.balanceConfig` -> `Assets/_Project/Data/Balance/V1_1/BalanceBootstrapConfig_v1_1.asset`
- `PoolSystem/GameManager.telemetryConfig` -> `Assets/_Project/Data/Balance/V1_1/BalanceTelemetryConfig_v1_1.asset`
- `PlayerRoot.combatScalingConfig` -> `Assets/_Project/Data/Balance/V1_1/CombatScalingConfig_v1_1.asset`
- `PlayerRoot/MainPlayer.BulletSpawner.combatScalingConfig` -> `Assets/_Project/Data/Balance/V1_1/CombatScalingConfig_v1_1.asset`

Follower setup uses runtime followers created from the main spawner template, so followers copy the main visual tiers and projectile count.

## Test results

Compile:

- `dotnet build "My project.sln" --no-restore`: passed with 0 errors.
- Existing warnings only: `System.Net.Http` assembly version conflict, obsolete Unity APIs/TMP APIs, and unused serialized fields.
- Unity MCP `unity_get_compilation_errors`: 0 errors, `isCompiling=false`.
- Unity MCP console error query: 0 errors.

Test runner:

- `dotnet test "TrueGate.PlayModeTests.csproj" --no-build`: exit code 0, but produced no Unity test count output.
- Full Unity EditMode and PlayMode Test Runner execution is not confirmed in this report; only C# compilation and `dotnet test` exit status were checked from the shell.

## Compile result

C# project compilation succeeded with 0 errors. Unity Editor compilation error query also returned 0 errors.

## PlayMode sanity result

Not completed. Scene references and visual thresholds were verified by MCP inspection, but no interactive no-meta/full-meta Play Mode run was performed.

## Build result

Android Development Build not completed in this pass. No Android player build result is claimed.

## Not yet verified by automated test

- Unity Test Runner EditMode and PlayMode counts.
- Runtime inspector sanity for no-meta and full-meta starts.
- Android Development Build.
- 5 full-meta real playtest runs.

## Telemetry from device

For Android Development Builds, collect files under the app persistent data path:

`BalanceTelemetry/balance-v1.1.0/`

Expected files:

- `run_summary.csv`
- `run_snapshot_15s.csv`
- `gate_events.jsonl`

Implementation complete for code-side Pass A changes; production balance pending real playtest telemetry.
