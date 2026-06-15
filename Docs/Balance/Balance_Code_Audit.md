# True Gate? Balance Code Audit

Audit date: 2026-06-14  
Target balance version: `balance-v1.0.0`  
Scope: read-only mapping of the current gameplay, progression, economy, and save code.

## Executive Summary

The current balance problem is caused by several systems multiplying each other:

- Every projectile currently carries the unit's full `Damage`.
- Every follower copies the main unit's full Damage, Fire Rate, HP, and Projectile Count.
- Permanent upgrades multiply each level by `1.5`.
- Gate offers can add, subtract, multiply, or divide the same runtime stats.
- The active `DefaultRunProgressionConfig.asset` caps enemies at 5 while the scene requests 10 visible enemies.

The repository already contains useful foundations:

- Enemy and bullet pooling.
- Local versioned JSON save with backup and PlayerPrefs migration.
- Run progression curves and enemy role weights.
- Enemy runtime scaling.
- Chomboom area damage to all player units inside its radius.
- Promotion that keeps the selected follower's remaining HP.

Balance v1 should extend these systems rather than replace them.

## Runtime Flow

```text
GameManager.StartRun
  -> MainPlayerUnit.Initialize
  -> PlayerMetaUpgradeService.ApplyToPlayer
  -> PlayerController.SetSquadCount
  -> RunStatsTracker.BeginRun
  -> EnemySpawnerSystem.BeginRun

PlayerController.Update
  -> ShootSquad
  -> each alive PlayerUnit.Shoot
  -> BulletSpawner.Shoot
  -> P bullets, each with full unit damage

Enemy death
  -> EnemySpawnerSystem.EnemyKilled
  -> RunStatsTracker.HandleEnemyKilled
  -> integer coin and score accumulation

Squad defeated
  -> GameManager.HandleSquadDefeated
  -> RunStatsTracker.EndRun
  -> SaveService.RecordRunResult
  -> wallet and best records saved
```

## System Map

| System | Current implementation | Hardcoded or data-driven | Balance v1 action |
|---|---|---|---|
| Base player stats | `PlayerUnitConfig`, `MainPlayerUnit`, `PlayerUnit` | Asset-backed | Keep asset as legacy source; introduce versioned meta config |
| Permanent upgrades | `PlayerMetaUpgradeService` | Hardcoded values, `1.5x`, and costs | Replace lookup math with `PlayerMetaBalanceConfig` |
| Fire cadence | `BulletSpawner.GetShotInterval()` | Direct `1 / rawFireRate` | Route through `BalanceV1Math.EffectiveFireRate` |
| Projectile count | `BulletSpawner.Shoot()` | Runtime field | Keep visual count; normalize damage per volley |
| Bullet damage | `BulletSpawner.Shoot()` and `Bullet.Init()` | Full damage per bullet | Calculate main/follower damage before spawning |
| Squad creation | `PlayerController.SetSquadCount()` | Runtime instantiate with prefab fallback | Keep structure; apply recruit HP ratio |
| Follower stats | `PlayerController.ConfigureFollower()` and `SyncFollowersFromMain()` | Full clone of main | Apply follower damage and HP scaling |
| Promotion | `PlayerController.HandleMainUnitDied()` | Code | Already preserves remaining HP; retain and test |
| Gate generation | `GateSystem` | Serialized rules plus runtime ScriptableObjects | Replace offer rules with versioned category entries |
| Gate application | `GateEffectApplier` | Five stat targets and four operations | Extend for typed effects and drawbacks |
| Enemy spawning | `EnemySpawnerSystem` | Asset curves plus scene values | Add pressure nodes and threat budget |
| Enemy scaling | `RunProgressionConfig` | Asset curves | Migrate into versioned pressure config |
| Chomboom AoE | `ChomboomController`, `ChomboomBoomFx` | Prefab fields | Already damages every nearby `PlayerUnit`; optimize and test |
| Score and run coin | `RunStatsTracker` | Integer accumulation | Separate float reward points from score |
| Wallet commit | `SaveService.RecordRunResult()` | End-of-run | Already commits at run end; preserve |
| Save/load | `SaveData`, `SaveService`, `LocalSaveRepository` | JSON with schema and backup | Add balance version and schema migration |
| Telemetry | Not implemented | None | Add local debug telemetry behind config |

## Player Stats And Permanent Upgrades

### Source of base stats

- `Assets/_Project/Data/Player/MainPlayerUnitConfig.asset`
- Base HP: `10`
- Base Damage: `1`
- Base Fire Rate: `4`
- Bullet Speed: `12`

`MainPlayerUnit.ApplyUnitConfig()` applies these values through `PlayerUnit` setters.

### Permanent upgrade application

`GameManager.StartRun()` calls:

```text
PlayerMetaUpgradeService.ApplyToPlayer(mainPlayerUnit, playerController)
```

`PlayerMetaUpgradeService` currently contains:

- Five levels.
- Upgrade multiplier `1.5`.
- Costs `100, 200, 500, 1500, 5000`.
- Base Projectile Count `5`.
- Base Squad Size `1`.

Whole-number stats are multiplied and rounded upward every level. This creates Projectile and Squad sequences that grow much faster than the planned v1 values.

### Reset behavior

The scene is reloaded for Home and Restart. At run start, `MainPlayerUnit.Initialize()` reapplies its unit config and the permanent upgrade service applies meta values. There is no dedicated immutable `RunStats` model yet.

Risk: calling `Initialize()` repeatedly on an already initialized unit reapplies the asset config. Phase 3 must preserve the existing run-start ordering.

## Fire Rate And Projectile Damage

`BulletSpawner` owns runtime Damage, Fire Rate, Projectile Count, Bullet Speed, and firing cooldown.

Current cadence:

```text
shotInterval = 1 / fireRate
```

Current volley:

```text
for each projectile:
    SpawnBullet(damage)
```

Therefore Projectile Count is a linear DPS multiplier. Split bullets can add additional damage through modifiers, so simulator and telemetry must distinguish base volley projectiles from child projectiles.

Recommended integration point:

- Keep `PlayerUnit.FireRate` as raw Fire Rate for UI and gates.
- Store or calculate effective Fire Rate in `BulletSpawner`.
- Give `BulletSpawner` a shooter damage scale so followers do not overwrite the main raw Damage value.
- Calculate normalized per-bullet damage once per volley.

## Squad And Promotion

`PlayerController` owns the main unit and follower list.

Current follower setup:

- Copies the main unit's Damage.
- Copies the main unit's raw Fire Rate.
- Copies the main unit's Max HP.
- Copies Projectile Count and bullet presentation.
- Restores full health when newly added.

Current promotion behavior is already compatible with the plan:

1. Select alive follower with highest current HP.
2. Copy its state into the existing main unit.
3. Preserve its current HP with a minimum of 1.
4. Remove and destroy the promoted follower object.

Required changes:

- Follower max HP becomes a ratio of main max HP.
- Follower bullet damage uses diminishing squad scale.
- Newly recruited followers start at the recruit HP ratio.
- Existing followers must retain proportional or clamped current HP when max HP changes.

The current runtime fallback uses `Instantiate` and `Destroy` for followers. This is acceptable for the first balance pass because recruitment is infrequent, but should be profiled before release.

## Gate System

Active scene values:

- Cadence: `12` seconds.
- Three lanes.
- Runtime offer generation enabled.
- Minimum buff ratio: `0.34`, resulting in at least two buff candidates for three gates due to `CeilToInt`.
- Projectile and Player Count cap: `50`.

Current targets:

- Damage
- Fire Rate
- Max HP
- Projectile Count
- Player Count

Current operations:

- Add
- Subtract
- Multiply
- Divide

The proposed Stable, Utility, Risky, and Major gates cannot be represented cleanly by the current enum pair. Effects such as Barrier, Freeze, Bounty, Cleanse, enemy pressure, timed drawbacks, and reroll require typed effect data and runtime state.

Phase 4 should preserve the current lane spawning, movement, pooling, and `GateLogic` flow while replacing only offer selection and effect execution.

## Enemy Spawning And Difficulty

`EnemySpawnerSystem` uses `DefaultRunProgressionConfig.asset` in `Main.unity`.

Important active-data mismatch:

- `Main.unity` sets `minimumVisibleEnemies = 10`.
- `DefaultRunProgressionConfig.asset` sets `maxActiveEnemiesCurve = 5` at every key.
- The C# class defaults show `80–220`, but those defaults are not the active serialized asset values.

Runtime result:

- `TopUpVisibleEnemies()` tries to reach 10 visible enemies.
- Active count reaches the cap of 5.
- Normal interval and batch spawning become mostly irrelevant after the initial top-up.

Current config supports only:

- Spawn interval.
- Batch size.
- Max active enemies.
- HP, movement, damage, and enemy projectile speed multipliers.
- Basic, Exploder, and Ranged unlock/weight rules.

Missing for v1:

- Minimum visible curve.
- Threat budget and threat cost.
- Swarmer, Tanker, and Elite progression roles.
- Explicit spawn-per-second pressure nodes.

Pooling exists, but `PoolSystem` creates additional instances when a queue is empty. Higher caps must be profiled because they can still allocate during play when pools are undersized.

## Enemy Roles And Chomboom

The active runtime roles inferred by component are:

- Basic melee.
- Chomboom exploder melee.
- Vomfy ranged.

Archetype assets also exist for Fodder, Swarmer, Tanker, Ranged, and Elite, but archetype data is not currently connected to `EnemyController` runtime selection. `EnemyController` reads `UnitData` or prefab fallback fields.

Chomboom already works as a squad counter:

- Proximity or death starts arming.
- `ChomboomBoomFx.ApplyDamageOnce()` finds all active `PlayerUnit` instances.
- Every alive unit within the radius takes damage.

No functional AoE rewrite is needed. A later optimization should replace `FindObjectsByType<PlayerUnit>()` with a non-alloc squad registry or physics query.

## Economy, Score, And Save

Current enemy rewards are integer values in:

- `EnemyController`
- `EnemyRuntimeStats`
- `EnemyArchetypeData`
- `RunStatsTracker`

`RunStatsTracker` currently:

- Adds one kill per enemy.
- Adds integer enemy coin reward.
- Adds integer enemy score.
- Does not add time score.

`SaveService.RecordRunResult()` is called only from `RunStatsTracker.EndRun()`. It updates best records and adds earned run coins to the wallet. This already satisfies the requirement that run earnings are committed at run end.

Save foundation:

- `save.json`
- Temporary file and backup file.
- `schemaVersion = 1`
- Revision and timestamp.
- PlayerPrefs migration.
- Cloud provider interface, currently using `NoOpCloudSaveProvider`.

Balance v1 save migration should add `balanceVersionLastPlayed` and increment schema only when serialized data changes. Upgrade levels can remain unchanged because v1 changes level values, not level ownership.

## UI Dependencies

`UISystem` reads wallet, best stats, and upgrade values through `SaveService` and `PlayerMetaUpgradeService`.

Changing upgrade values to data-driven config must preserve the public service methods used by the UI:

- `GetLevel`
- `GetCost`
- `GetCurrentValue`
- `GetNextValue`
- `FormatValue`
- `TryPurchase`

This avoids rebuilding or rebinding UpgradePanel during the balance pass.

## Existing Risks

1. Projectile and squad scaling are the primary power explosion.
2. The active spawn cap is 5 despite higher C# defaults.
3. Runtime gate ScriptableObjects are created repeatedly and never explicitly destroyed.
4. Chomboom uses global object searches for proximity and explosion.
5. Pool exhaustion allocates during gameplay.
6. Enemy archetype assets are presently disconnected from most active prefabs.
7. Coin rewards cannot represent fractional fodder rewards.
8. No test assembly currently covers balance math or run flow.
9. No telemetry or balance version is attached to run results.

## Phase Dependencies

```text
Config classes
  -> Balance math
  -> Player projectile and squad integration
  -> Spawn pressure and role data
  -> Gate categories and typed effects
  -> Economy migration
  -> Telemetry
  -> Tests, simulator, and tuning
```

Gate and economy values must not be tuned before the combat math is integrated, because current Projectile and Squad multiplication would invalidate those results.

## Audit Conclusion

Balance v1 is implementable without replacing the core architecture. The safest approach is to add versioned configs and compatibility facades, then change one runtime path at a time. Scene and prefab references should only be changed through Unity MCP after the corresponding scripts compile.

