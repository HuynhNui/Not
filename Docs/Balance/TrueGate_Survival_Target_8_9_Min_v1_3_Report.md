# True Gate Survival Target 8-9 Min v1.3 Report

## Implemented

- Added balance candidate `balance-v1.3.0-survival-bridge`.
- Added run-cap benchmark profile `run-cap-ceiling-v1`:
  - Damage `3.50`
  - Fire Rate `8.50`
  - Max HP `36`
  - Projectile `9`
  - Squad `16`
- Added debug/editor benchmark selector via `BalanceBenchmarkPreset`.
- Wired `Assets/_Project/Scenes/Main.unity` to:
  - `Assets/_Project/Data/Balance/V1_3_Survival/BalanceBootstrapConfig_v1_3_survival_bridge.asset`
- Kept reused assets from v1.2.1 where requested:
  - player meta benchmark config
  - combat scaling
  - run pressure
  - gate pool
  - economy
  - enemy roles

## Gate Progression Changes

- Phase starts changed to `0 / 90 / 180 / 300 / 420`.
- Major pity changed to force after `1` eligible miss.
- Bullet Storm now gives permanent projectile count with a temporary damage drawback.
- Reinforcement now gives permanent squad size with temporary enemy pressure.
- Major Projectile and Major Recruit are strengthened to `+2 / +2 / +2 / +3 / +3`.
- Stable offer weights are phase-aware:
  - `stable_damage`: `1.0 / 1.5 / 2.0 / 2.0 / 1.5`
  - `stable_fire_rate`: `1.0 / 1.5 / 2.0 / 2.0 / 1.5`
  - `stable_vitality`: `1.0 / 1.0 / 1.25 / 1.5 / 2.0`
- `risky_bounty` is filtered out in benchmark mode only.

## Telemetry Additions

- Snapshots now include squad HP totals, active cap, minimum visible enemies, raw spawn rate, threat budget, active/visible ratios, and gate pressure multipliers.
- `gate_selected` events now include before/after combat stats, estimated effective DPS, and `wasCapped`.
- Run summary now includes ending combat stats, ending effective DPS, squad HP totals, and final pressure multipliers.

## Verification

- `dotnet build "My project.sln" --no-restore -v:minimal`: passed with warnings only.
- Unity reflection test: `GateScalingProfileTests passed=6`.
- `dotnet test "My project.sln" --no-build --no-restore -v:minimal`: exited `0`, but produced no detailed test-case output.

## Remaining Human Validation

This change should not be considered a confirmed 8-9 minute balance result yet. Run the cap-start benchmark profile at least three times, then full-meta benchmark runs, and compare survival time plus the telemetry checkpoints at `300 / 420 / 480 / 540` seconds.
