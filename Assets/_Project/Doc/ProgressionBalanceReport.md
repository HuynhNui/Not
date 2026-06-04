# Progression Balance Report - V1

Target feel: dong, de giet. The field should fill quickly, enemies should die often, and the player should feel strong without spending long stretches shooting into an empty lane.

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

Problem before this tuning pass:

| Problem | Effect |
| --- | --- |
| Spawner created 1 enemy per interval | Start pressure was about 44 enemies/min, below fresh player kill capacity. |
| Gate offers included x2 Damage, Projectile Count, and Player Count | Early Gate choices could multiply DPS too sharply. |
| Vomfy used shared `EnemyUnitData` | Ranged enemy unintentionally used basic HP 3 instead of intended HP 5. |
| Meta upgrades come from PlayerPrefs | Local playtests may start much stronger than a fresh install. |

## Implemented Target Curve

Enemy density now comes from interval plus batch size. Spawn/min is approximate and assumes active cap is not already full.

| Time | Interval | Batch | Spawn/min | Max active | Intended feel |
| ---: | ---: | ---: | ---: | ---: | --- |
| 0s | 1.30 | 2 | 92 | 45 | Field fills in the first 10 seconds. |
| 60s | 1.10 | 2 | 109 | 60 | Player kills often; basic melee still dominates. |
| 180s | 0.90 | 2-3 | 170-200 | 85 | Chomboom/ranged pressure is visible. |
| 300s | 0.80 | 3 | 225 | 110 | Horde pressure should approach player clear rate. |
| 420s | 0.70 | 3 | 257 | 130 | Strong Gate choices needed to stay comfortable. |
| 720s | 0.65 | 3-4 | 277-369 | 150 | Endless pressure, performance cap still enforced. |

Enemy role mix:

| Time | Basic melee | Chomboom | Ranged | Notes |
| ---: | ---: | ---: | ---: | --- |
| 0s | 100% | 0% | 0% | Only basic enemies at run start. |
| 60s | ~92% | ~8% | 0% | Chomboom unlocks at 45s. |
| 180s | ~79% | ~12% | ~9% | Ranged is present but not dominant. |
| 300s | ~71% | ~16% | ~13% | Mixed pressure, still horde-first. |
| 420s | ~65% | ~19% | ~17% | Late game has more special pressure. |

Enemy durability:

| Time | HP mult | Basic HP | Ranged HP | Design note |
| ---: | ---: | ---: | ---: | --- |
| 0s | 1.00 | 3.0 | 5.0 | Basic is 2-3 hits for a fresh player. |
| 60s | 1.20 | 3.6 | 6.0 | Still easy to clear with one good Gate. |
| 180s | 1.75 | 5.3 | 8.8 | Density matters more than tankiness. |
| 300s | 2.40 | 7.2 | 12.0 | Player needs accumulated Gate power. |
| 420s | 3.00 | 9.0 | 15.0 | Late enemies stop being disposable. |

## Player Power Projection

Formula:

`TheoreticalDPS = Damage * FireRate * ProjectileCount * AliveSquadCount`

Fresh install, no meta:

| Time | Example state | DPS | Basic kills/min at same-time HP |
| ---: | --- | ---: | ---: |
| 0s | 1 damage, 4 fire, 1 projectile, 1 unit | 4 | 80 |
| 60s | likely additive Gates: 2 damage, 5 fire, 2 projectiles, 1 unit | 20 | 333 |
| 180s | likely good Gates: 4 damage, 6 fire, 2 projectiles, 2 units | 96 | 1097 |
| 300s | strong Gates: 6 damage, 7 fire, 3 projectiles, 2 units | 252 | 2100 |

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
- Spawn uses small batches, not a single enemy per interval.
- Basic melee remains the majority enemy so the game keeps a horde-kill rhythm.
- Chomboom and ranged enemies add movement pressure after unlocks, not boss-style spikes.
- Gate generation no longer offers x2 Damage, x2 Projectile Count, or x2 Player Count from runtime random offers.
- Ranged enemy now has its own `RangedEnemyUnitData` so its base HP is 5 before scaling.

## Acceptance Checklist

| Scenario | Expected result |
| --- | --- |
| 0-10s | Screen starts filling; player has frequent kills almost immediately. |
| 0-60s | Spawn/min is above fresh-player no-Gate kill capacity, but weak enemies keep the feel satisfying. |
| 90-180s | Chomboom and ranged enemies appear without overtaking basic melee. |
| 3-5 minutes | Active enemy count often approaches cap, but object pooling prevents allocation spikes. |
| Gate choices | Additive upgrades are common; early multiplicative DPS runaway is reduced. |

## Next Measurement Pass

Add or manually record these values during playtest:

- Kills per minute.
- Average active enemy count.
- Time spent at max active cap.
- Gate choices taken.
- Player damage, fire rate, projectile count, and squad count at 60s/180s/300s.
- FPS when active enemy count is near cap.
