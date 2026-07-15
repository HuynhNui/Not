# True Gate Damage-Forward Meta v1.3.2 Report

## Implemented

- Added candidate balance version `balance-v1.3.2-damage-forward-meta`.
- Created versioned assets under:
  - `Assets/_Project/Data/Balance/V1_3_2_DamageForwardMeta/`
- Wired `Assets/_Project/Scenes/Main.unity` to:
  - `BalanceBootstrapConfig_v1_3_2_DamageForwardMeta.asset`
- Updated the full-meta benchmark profile:
  - Damage `3.00`
  - Fire Rate `6.40`
  - Max HP `20`
  - Projectile `6`
  - Squad `12`

## Permanent Meta Damage

Permanent Damage now uses the damage-forward meta progression:

| Level | Damage | Fire | HP | Projectile | Squad | Cost |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 1.00 | 4.0 | 10.0 | 1 | 1 | 0 |
| 1 | 1.40 | 4.4 | 11.5 | 2 | 2 | 100 |
| 2 | 1.80 | 4.8 | 13.0 | 4 | 3 | 250 |
| 3 | 2.20 | 5.2 | 15.0 | 6 | 5 | 550 |
| 4 | 2.60 | 5.8 | 17.5 | 6 | 8 | 1100 |
| 5 | 3.00 | 6.4 | 20.0 | 6 | 12 | 2200 |

Bullet max level remains `3`.

## Cap Changes

Run caps now use the v1.3.2 damage-forward meta target:

| Stat | v1.3.1 cap | v1.3.2 cap |
|---|---:|---:|
| Damage | 6.50 | 6.00 |
| Fire Rate | 7.00 | 7.00 |
| Max HP | 36 | 36 |
| Projectile | 7 | 7 |
| Squad | 13 | 13 |

Modifier safety caps are unchanged.

## Gate Compatibility

- Major Projectile is `+1` in every phase.
- Major Recruit is `+1` in every phase.
- Bullet Storm is Projectile `+1` in every phase with the same temporary damage drawbacks.
- Reinforcement is Squad `+1` in every phase with the same temporary pressure drawbacks.
- Damage, Glass Cannon, and Overclock damage magnitudes are unchanged.
- Fire remains clamped at `7.00`.

## Benchmarks

The bootstrap selector supports:

- `FullMetaStart`
- `OldRunCapStart`
- `DamageForwardCapStart`

New profiles:

- `full-meta-damage-forward-v1`: `3.00 / 6.40 / 20 / 6 / 12`
- `damage-forward-cap-6-v1`: `6.00 / 7.00 / 36 / 7 / 13`

## Telemetry

Estimated base projectile emissions remain exported with:

```text
EffectiveFireRate * ProjectileCount * SquadCount
```

Recorded in:

- 15-second snapshots
- run summary peak
- `gate_selected` before/after event data
- benchmark export CSV

This estimate excludes child/split bullets, so it is a base visual-load proxy rather than exact projectile count.

## Export

Exported files:

- `Tools/Balance/output/balance-v1.3.2-damage-forward-meta/true_gate_balance-v1.3.2-damage-forward-meta.json`
- `Tools/Balance/output/balance-v1.3.2-damage-forward-meta/gate_phase_values.csv`
- `Tools/Balance/output/balance-v1.3.2-damage-forward-meta/benchmark_target_curve.csv`

Benchmark curve confirms:

- Full-meta start DPS: about `282.120`
- Full-meta start emissions: about `459.871/s`
- Damage-forward cap DPS: about `652.840`
- Damage-forward cap emissions: about `630/s`

Human playtest is still required before claiming the 8-9 minute target.
