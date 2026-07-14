# True Gate Projectile & Visual Hotfix v1.1.1 Report

Status: Hotfix implementation complete; Unity Test Runner counts and Android build remain pending.

## Root-cause correction

Base runtime projectile count `1` is intentional design. The regression was introduced when the meta projectile table became `5/6/8/10/13/16` and `ApplyToPlayer()` correctly started using that data-driven table. That made no-meta level 0 spawn 5 projectiles.

The hotfix keeps `ApplyToPlayer()` data-driven and changes the projectile meta table to:

`1 / 2 / 3 / 4 / 5 / 6`

`CombatScalingConfig.baseProjectileCount` remains `5` as the damage normalization anchor.

## Visual regression

Tier 00 threshold had been changed from `0` to `1.30`. Base Damage `1.00` matched no tier, so runtime fell back to serialized `bulletPrefab`, which was still `Assets/_Project/Prefabs/Bullets/Bullet.prefab`.

Before state captured by Unity MCP:

- Fallback: `Bullet`, `Assets/_Project/Prefabs/Bullets/Bullet.prefab`, GUID `160e2a92faff0f043aa475a569f9d2e4`
- Tier 0: `1.30 -> Bullet_Tier_00`
- Tier 1: `1.60 -> Bullet_Tier_10`
- Tier 2: `1.90 -> Bullet_Tier_20`
- Tier 3: `2.50 -> Bullet_Tier_50`
- Tier 4: `3.25 -> Bullet_Tier_100`

After state:

| minDamage | Official prefab |
|---:|---|
| 0.00 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_00.prefab` |
| 1.30 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_10.prefab` |
| 1.60 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_20.prefab` |
| 1.90 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_50.prefab` |
| 2.50 | `Assets/_Project/Prefabs/Bullets/Bullet_Tier_100.prefab` |

Fallback `bulletPrefab` now points to `Bullet_Tier_00`.

## Files changed

- `Assets/_Project/Scripts/Data/Balance/CombatScalingConfig.cs`
- `Assets/_Project/Scripts/Data/Balance/PlayerMetaBalanceConfig.cs`
- `Assets/_Project/Scripts/Systems/ProgressionSystem/PlayerMetaUpgradeService.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/BulletSpawner.cs`
- `Assets/_Project/Tests/Editor/BalanceV1MathTests.cs`
- `Assets/_Project/Tests/PlayMode/BalanceRuntimePlayModeTests.cs`
- `Assets/_Project/Scenes/Main.unity`
- `Assets/_Project/Data/Balance/V1_1_1/CombatScalingConfig_v1_1_1.asset`
- `Assets/_Project/Data/Balance/V1_1_1/PlayerMetaBalanceConfig_v1_1_1.asset`
- `Assets/_Project/Data/Balance/V1_1_1/BalanceTelemetryConfig_v1_1_1.asset`
- `Assets/_Project/Data/Balance/V1_1_1/BalanceBootstrapConfig_v1_1_1.asset`

Unrelated existing file left untouched:

- `Assets/Front/Upheaval_TMP.asset`

## Config

Active balance version is now `balance-v1.1.1`.

Kept from Pass A:

- Damage table: `1.00 / 1.15 / 1.30 / 1.50 / 1.70 / 1.90`
- Projectile coverage coefficient: `0.20`
- Squad coverage coefficient: `0.55`
- Combat math base projectile count: `5`
- Fire Rate, Max HP, Squad Size, and Costs unchanged
- Enemy, economy, gate cadence, and final cutscene unchanged

## Runtime evidence

Play Mode sanity via Unity MCP using temp save data:

- No meta: `damage=1`, `projectile=1`, `squad=1`, `tierIndex=0`, `prefab=Bullet_Tier_00`
- Full meta: `damage=1.9`, `projectile=6`, `squad=12`, `tierIndex=3`, `prefab=Bullet_Tier_50`
- Damage `2.5`: `projectile=6`, `tierIndex=4`, `prefab=Bullet_Tier_100`
- Full-meta Projectile Gate: `6 -> 7`
- New-run apply after gate: `7 -> 6`

Official prefab validation:

- `Bullet_Tier_00`: Bullet=True, SpriteRenderer=True, Collider2D=True, Rigidbody2D=True
- `Bullet_Tier_10`: Bullet=True, SpriteRenderer=True, Collider2D=True, Rigidbody2D=True
- `Bullet_Tier_20`: Bullet=True, SpriteRenderer=True, Collider2D=True, Rigidbody2D=True
- `Bullet_Tier_50`: Bullet=True, SpriteRenderer=True, Collider2D=True, Rigidbody2D=True
- `Bullet_Tier_100`: Bullet=True, SpriteRenderer=True, Collider2D=True, Rigidbody2D=True

Pool verification:

- `PoolSystem.Spawn(prefab, ...)` uses prefab object as dynamic key and creates a pool if one does not exist.
- Existing prewarm still includes legacy `Bullet.prefab`, but MainPlayer no longer references it. Official tier prefabs can still spawn through dynamic pool creation.

## Tests and compile

- `dotnet build "My project.sln" --no-restore`: passed with 0 errors.
- Unity MCP compilation errors: 0, `isCompiling=false`.
- `dotnet test "TrueGate.PlayModeTests.csproj" --no-build`: exit code 0, but no test count output.
- Unity batchmode Test Runner attempt was blocked because the project is already open in Unity Editor:
  `Multiple Unity instances cannot open the same project.`

Unity console:

- Validation initially logged the pre-hotfix fallback `Bullet.prefab` as an error, as intended.
- After wiring and clearing the old buffer, Unity console error query returned 0 errors.
- During Play Mode, Unity Android Remote emitted `adb.exe: no devices/emulators found`; this is an environment/device issue, not a gameplay hotfix error. Console was cleared afterward and error query returned 0.

## Not completed

- Full Unity EditMode Test Runner with count.
- Full Unity PlayMode Test Runner with count.
- Inspector before/after screenshots. Before state was captured as serialized MCP evidence instead.
- Android Development Build.

## Acceptance checklist

- No-meta starts with 1 projectile: verified by Play Mode sanity.
- Meta projectile table is `1/2/3/4/5/6`: code and v1.1.1 asset updated.
- Full-meta starts with 6 projectile: verified by Play Mode sanity.
- Projectile Gate moves 6 to 7: verified by Play Mode sanity.
- Restart/new-run apply resets 7 to 6: verified by Play Mode sanity.
- Damage 1.00 uses `Bullet_Tier_00`: verified by Play Mode sanity.
- Fallback prefab is `Bullet_Tier_00`: verified by scene reload inspection.
- Only official `Bullet_Tier_X` prefabs are referenced by MainPlayer BulletSpawner: verified by scene reload inspection.
- Tier mapping is `0/1.30/1.60/1.90/2.50`: verified by scene reload inspection.
- `baseProjectileCount` math anchor remains 5: verified in `CombatScalingConfig_v1_1_1`.
- Damage progression Pass A preserved: verified in code/assets.
- Enemy, economy, gate cadence, and final cutscene not changed in this hotfix.
