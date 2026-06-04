# Progression Balance Report - V3

Target feel: horde mode - dong, de giet, tran man hinh. The field should fill quickly, enemies should die often, and the player should feel strong while fighting a dense crowd instead of a sparse lane.

Performance warning: this target is now roughly 100 raw spawn requests/second from run start, or about 6000/min if cap room exists. Actual enemy count stops at the active cap for mobile safety. Profile on mobile before locking these caps.

Visibility rule: normal fallback spawn is top-only. The spawner also keeps a floor of 10 visible enemies by spawning top-band enemies inside the camera when the visible count drops too low. Enemies outside the camera viewport do not take player bullet damage.

## Current Diagnosis

Fresh player baseline:

| Stat | Value | Notes |
| --- | ---: | --- |
| Damage | 1 | From `MainPlayerUnitConfig` |
| Fire rate | 4/s | Synced into `BulletSpawner` |
| Projectile count | 1 | Main unit only at run start |
| Base theoretical DPS | 4 | `damage * fireRate * projectileCount * squadCount` |
| Basic enemy HP | 3 | From `EnemyUnitData` |
| Basic kill capacity | 80/min | `4 DPS / 3 HP * 60` if every shot hits |

Problem before horde tuning:

| Problem | Effect |
| --- | --- |
| Spawner created too few enemies per second | Start pressure was below the desired horde fantasy. |
| Gate offers included x2 Damage, Projectile Count, and Player Count | Early Gate choices could multiply DPS too sharply. |
| Vomfy used shared `EnemyUnitData` | Ranged enemy unintentionally used basic HP 3 instead of intended HP 5. |
| Meta upgrades come from PlayerPrefs | Local playtests may start much stronger than a fresh install. |

## Implemented Target Curve

Enemy density now comes from interval plus batch size. Raw spawn/min is approximate and assumes active cap is not already full.

| Time | Interval | Batch | Raw spawn/min | Max active | Intended feel |
| ---: | ---: | ---: | ---: | ---: | --- |
| 0s | 0.10 | 10 | 6000 | 360 | 100 enemies/sec raw from the first wave, capped for mobile. |
| 60s | 0.09 | 10 | 6667 | 420 | Field should hit cap quickly if player is not clearing fast enough. |
| 180s | 0.08 | 10 | 7500 | 520 | Chomboom/ranged pressure is visible but horde-first. |
| 300s | 0.07 | 10 | 8571 | 620 | Active count should often approach cap. |
| 420s | 0.06 | 10 | 10000 | 720 | Late game becomes a real screen-flood test. |
| 720s | 0.06 | 15 | 15000 | 800 | Extreme endless pressure; reduce cap first if FPS drops. |

Enemy role mix:

| Time | Basic melee | Chomboom | Ranged | Notes |
| ---: | ---: | ---: | ---: | --- |
| 0s | 100% | 0% | 0% | Only basic enemies at run start. |
| 60s | ~96% | ~4% | 0% | Chomboom unlocks at 45s, still rare. |
| 180s | ~89% | ~7% | ~4% | Ranged is present but not dominant. |
| 300s | ~82% | ~11% | ~7% | Mixed pressure, basic still carries the horde. |
| 420s | ~74% | ~15% | ~11% | Specials matter, but the screen is still mostly melee. |

Enemy durability:

| Time | HP mult | Basic HP | Ranged HP | Design note |
| ---: | ---: | ---: | ---: | --- |
| 0s | 1.00 | 3.0 | 5.0 | Basic is 2-3 hits for a fresh player. |
| 60s | 1.05 | 3.2 | 5.3 | Still easy to clear with one good Gate. |
| 180s | 1.20 | 3.6 | 6.0 | Density matters more than tankiness. |
| 300s | 1.45 | 4.4 | 7.3 | Player needs accumulated Gate power. |
| 420s | 1.75 | 5.3 | 8.8 | Late enemies are stronger, but not spongey. |
| 720s | 2.20 | 6.6 | 11.0 | Endless pressure ramps without turning every enemy into a wall. |

Enemy damage:

| Time | Damage mult | Design note |
| ---: | ---: | --- |
| 0s | 0.75 | Huge density creates pressure without instant contact deaths. |
| 60s | 0.85 | Early mistakes are punished softly. |
| 180s | 1.00 | Normal damage resumes once the player has Gates. |
| 300s | 1.15 | Mid-run pressure rises gradually. |
| 420s | 1.30 | Late density plus damage becomes dangerous. |
| 720s | 1.55 | Endless pressure, still below the old 1.75 late spike. |

## Player Power Projection

Formula:

`TheoreticalDPS = Damage * FireRate * ProjectileCount * AliveSquadCount`

Fresh install, no meta:

| Time | Example state | DPS | Basic kills/min at same-time HP |
| ---: | --- | ---: | ---: |
| 0s | 1 damage, 4 fire, 1 projectile, 1 unit | 4 | 80 |
| 60s | likely additive Gates: 2 damage, 5 fire, 2 projectiles, 1 unit | 20 | 381 |
| 180s | likely good Gates: 4 damage, 6 fire, 2 projectiles, 2 units | 96 | 1600 |
| 300s | strong Gates: 6 damage, 7 fire, 3 projectiles, 2 units | 252 | 3476 |

The table is intentionally theoretical. Real kills are lower because bullets can miss, enemies enter from limited lanes, projectile lifetime matters, and target overlap is uneven. The useful signal is the gap: player scaling can explode, so density must rise first and multiplicative Gate offers must stay controlled.

Example local meta impact:

| Meta example | Start DPS impact |
| --- | ---: |
| No meta | 4 DPS |
| Damage Lv2, Fire Rate Lv2 | 13.2 DPS |
| Damage Lv2, Fire Rate Lv2, Projectile Lv1 | 26.4 DPS |

If a local test account already has meta upgrades, the first minute can feel much easier than a new player run.

## Balance Decisions

- First pass prioritizes density over enemy tankiness.
- Spawn targets 100 raw enemies/sec from run start, then rises if active cap allows.
- Actual spawn stops at active cap for mobile safety; no far-enemy recycling in this pass.
- Camera fallback spawn is top-only; side edge spawn was removed because it felt directionally wrong.
- Visible-floor top-up keeps at least 10 enemies inside the camera when active cap allows.
- Enemies outside the camera viewport ignore player bullet damage, and offscreen bullet hits do not trigger hit modifiers.
- Basic melee remains the majority enemy so the game keeps a horde-kill rhythm.
- Chomboom and ranged enemies add movement pressure after unlocks, not boss-style spikes.
- Gate generation no longer offers x2 Damage, x2 Projectile Count, or x2 Player Count from runtime random offers.
- Ranged enemy now has its own `RangedEnemyUnitData` so its base HP is 5 before scaling.

## Acceptance Checklist

| Scenario | Expected result |
| --- | --- |
| 0-10s | Enemies spawn from top only; roughly 1000 enemies are requested in the first 10 seconds if cap room exists. |
| Visible floor | If fewer than 10 active enemies are inside the camera, the spawner fills from the top visible band until 10 or cap. |
| Offscreen damage | Player bullets do not reduce enemy HP while the enemy is outside the camera viewport. |
| 0-60s | Raw spawn target is about 6000-6667/min, far above fresh-player no-Gate kill capacity. |
| 45-90s | Chomboom appears after 45s, ranged appears after 75s, basic remains visually dominant. |
| 3-5 minutes | Active enemy count often approaches 520-620, but object pooling prevents allocation spikes. |
| Mobile safety | Active enemy count never exceeds the current cap; lower cap first if FPS drops. |
| Gate choices | Additive upgrades are common; early multiplicative DPS runaway is reduced. |

## Next Measurement Pass

Add or manually record these values during playtest:

- Kills per minute.
- Average active enemy count.
- Time spent at max active cap.
- Gate choices taken.
- Player damage, fire rate, projectile count, and squad count at 60s/180s/300s.
- FPS when active enemy count is near 360, 520, and 720.
- GC allocation or visible hitching when active enemy count first exceeds 360.
