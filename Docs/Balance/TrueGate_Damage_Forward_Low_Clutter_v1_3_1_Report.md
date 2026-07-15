# True Gate Damage-Forward Low-Clutter v1.3.1 Report

## Implemented

- Added candidate balance version `balance-v1.3.1-damage-forward`.
- Created versioned assets under:
  - `Assets/_Project/Data/Balance/V1_3_1_DamageForward/`
- Wired `Assets/_Project/Scenes/Main.unity` to:
  - `BalanceBootstrapConfig_v1_3_1_damage_forward.asset`
- Kept full-meta benchmark start unchanged:
  - Damage `1.90`
  - Fire Rate `6.40`
  - Max HP `20`
  - Projectile `6`
  - Squad `12`

## Cap Changes

Run caps now use the damage-forward low-clutter target:

| Stat | v1.3.0 cap | v1.3.1 cap |
|---|---:|---:|
| Damage | 3.50 | 6.50 |
| Fire Rate | 8.50 | 7.00 |
| Max HP | 36 | 36 |
| Projectile | 9 | 7 |
| Squad | 16 | 13 |

Modifier safety caps are unchanged.

## Gate Compatibility

- Major Projectile is `+1` in every phase.
- Major Recruit is `+1` in every phase.
- Bullet Storm is Projectile `+1` in every phase with the same temporary damage drawbacks.
- Reinforcement is Squad `+1` in every phase with the same temporary pressure drawbacks.
- Damage, Glass Cannon, and Overclock damage magnitudes are unchanged.

## Benchmarks

The bootstrap selector now supports:

- `FullMetaStart`
- `OldRunCapStart`
- `DamageForwardCapStart`

New cap-start profile:

- `damage-forward-cap-v1`
- `6.50 / 7.00 / 36 / 7 / 13`

## Telemetry

Added estimated base projectile emissions:

```text
EffectiveFireRate * ProjectileCount * SquadCount
```

Recorded in:

- 15-second snapshots
- run summary peak
- `gate_selected` before/after event data

This estimate excludes child/split bullets, so it is a base visual-load proxy rather than exact projectile count.

## Verification

- `dotnet build "My project.sln" --no-restore -v:minimal`: passed with warnings only.
- Unity reflection: `GateScalingProfileTests passed=9`.
- `dotnet test "My project.sln" --no-build --no-restore -v:minimal`: exited `0`, but produced no detailed case output.

## Output

Exported files:

- `Tools/Balance/output/balance-v1.3.1-damage-forward/true_gate_balance-v1.3.1-damage-forward.json`
- `Tools/Balance/output/balance-v1.3.1-damage-forward/gate_phase_values.csv`
- `Tools/Balance/output/balance-v1.3.1-damage-forward/benchmark_target_curve.csv`

Benchmark curve confirms:

- Full-meta start emissions: about `459.871/s`
- Damage-forward cap emissions: about `630/s`
- Damage-forward cap DPS: about `707.243`

Human playtest is still required before claiming the 8-9 minute target.
