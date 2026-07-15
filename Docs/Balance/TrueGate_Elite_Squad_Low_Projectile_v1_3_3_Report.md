# True Gate Elite Squad Low Projectile v1.3.3 Report

## Implemented

- Added candidate balance version `balance-v1.3.3-elite-squad`.
- Created versioned assets under:
  - `Assets/_Project/Data/Balance/V1_3_3_EliteSquad/`
- Wired `Assets/_Project/Scenes/Main.unity` to:
  - `BalanceBootstrapConfig_v1_3_3_EliteSquad.asset`
- Added `SquadPowerModel` with:
  - `DiminishingFollowers`
  - `EqualStrengthUnits`

## Elite Squad Model

The v1.3.3 combat asset uses `EqualStrengthUnits`:

- Follower damage scale: `1.0`
- Follower HP scale: `1.0`
- Squad offensive factor: linear squad count
- Recruit spawn current HP ratio: `0.60`

Recruit spawn HP is now only a starting-current-HP condition. Follower max HP and combat stats mirror the main unit.

## Permanent Meta

Permanent Damage remains:

| Level | Damage |
|---:|---:|
| 0 | 1.00 |
| 1 | 1.40 |
| 2 | 1.80 |
| 3 | 2.20 |
| 4 | 2.60 |
| 5 | 3.00 |

Projectile internal max level is now `2`:

| Level | Projectile |
|---:|---:|
| 0 | 1 |
| 1 | 2 |
| 2 | 3 |

Squad internal max level is now `3`:

| Level | Squad |
|---:|---:|
| 0 | 1 |
| 1 | 2 |
| 2 | 3 |
| 3 | 4 |

Save schema was incremented to `8` and migrates old Bullet `3-5 -> 2`, old Player `4-5 -> 3` without resetting wallet, story, tutorial, or other upgrade progress.

## Run Caps

| Stat | v1.3.3 cap |
|---|---:|
| Damage | 6.00 |
| Fire Rate | 7.00 |
| Max HP | 36 |
| Projectile | 4 |
| Squad | 5 |

Projectile and squad gates remain `+1` in every phase and are filtered after reaching these caps.

## Benchmarks

New profiles:

- `full-meta-elite-squad-v1`: `3.00 / 6.40 / 20 / 3 / 4`
- `elite-squad-cap-v1`: `6.00 / 7.00 / 36 / 4 / 5`

Export confirms:

- Full-meta start DPS: about `344.595`
- Full-meta start emissions: about `76.645/s`
- Elite-squad cap DPS: about `989.432`
- Elite-squad cap emissions: about `138.462/s`

The emission estimate excludes child/split bullets.

## Output

Exported files:

- `Tools/Balance/output/balance-v1.3.3-elite-squad/true_gate_balance-v1.3.3-elite-squad.json`
- `Tools/Balance/output/balance-v1.3.3-elite-squad/gate_phase_values.csv`
- `Tools/Balance/output/balance-v1.3.3-elite-squad/benchmark_target_curve.csv`

Human playtest is still required before claiming the 8-9 minute target.
