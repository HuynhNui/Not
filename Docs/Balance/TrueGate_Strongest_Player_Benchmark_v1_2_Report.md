# TrueGate Strongest Player Benchmark v1.2 Report

## Root Findings

- Current `Main` scene now points `GameManager.balanceConfig` at `balance-v1.2.0-benchmark`.
- Benchmark is isolated through `BalanceBenchmarkProfile`: it is active only in Editor/debug builds and ignored by release builds.
- The benchmark start state is damage `1.90`, fire rate `6.40`, max HP `20`, projectile count `6`, squad size `12`.
- Base enemy pressure, economy, enemy role assets, and base gate pool assets were reused unchanged.

## Files Changed

- `Assets/_Project/Scripts/Data/Balance/GateScalingProfile.cs`
- `Assets/_Project/Scripts/Data/Balance/BalanceBenchmarkProfile.cs`
- `Assets/_Project/Scripts/Systems/Balance/GateEffectPreview.cs`
- `Assets/_Project/Scripts/Systems/GateSystem/GateSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Gates/GateRuntimeEffectController.cs`
- `Assets/_Project/Scripts/Core/GameLoop/GameManager.cs`
- `Assets/_Project/Scripts/Systems/RunStatsSystem/RunStatsTracker.cs`
- `Assets/_Project/Scripts/Systems/Telemetry/BalanceTelemetryService.cs`
- `Assets/_Project/Editor/BalanceConfigExporter.cs`
- `Assets/_Project/Tests/Editor/GateScalingProfileTests.cs`
- `Assets/_Project/Scenes/Main.unity`

## Asset Wiring

- Bootstrap: `Assets/_Project/Data/Balance/V1_2_Benchmark/BalanceBootstrapConfig_v1_2_benchmark.asset`
- Gate scaling: `Assets/_Project/Data/Balance/V1_2_Benchmark/GateScalingProfile_v1_2_benchmark.asset`
- Benchmark profile: `Assets/_Project/Data/Balance/V1_2_Benchmark/BalanceBenchmarkProfile_full_meta.asset`
- Telemetry config: `Assets/_Project/Data/Balance/V1_2_Benchmark/BalanceTelemetryConfig_v1_2_benchmark.asset`
- Active version: `balance-v1.2.0-benchmark`

## Phase Table

| Phase | Start | Stable Damage | Stable Fire | Vitality | Repair | Barrier | Freeze | Major Projectile | Major Recruit | Overclock |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: | ---: | --- |
| early | 0s | x1.10 | +0.20 | x1.08 | 20% | 1 hit / 15s | x0.75 / 20s | +1 | +1 | dmg x1.15, fire x1.08 |
| growth | 120s | x1.12 | +0.30 | x1.10 | 25% | 1 hit / 18s | x0.72 / 22s | +1 | +1 | dmg x1.20, fire x1.10 |
| pressure | 240s | x1.15 | +0.40 | x1.12 | 30% | 2 hit / 18s | x0.68 / 25s | +2 | +2 | dmg x1.25, fire x1.12 |
| late | 360s | x1.18 | +0.50 | x1.15 | 40% | 2 hit / 22s | x0.64 / 28s | +2 | +2 | dmg x1.30, fire x1.15 |
| endgame | 480s | x1.20 | +0.60 | x1.18 | 50% | 3 hit / 25s | x0.60 / 30s | +2 | +2 | dmg x1.35, fire x1.18 |

Risky gates also scale by phase: glass cannon damage `1.25/1.30/1.35/1.40/1.45` with incoming drawback `1.20/1.18/1.16/1.14/1.12`; bullet storm projectile `1/1/1/2/2` with damage drawback `0.88/0.90/0.92/0.94/0.96`; reinforcement squad `1/1/1/2/2` with enemy pressure drawback `1.15/1.13/1.10/1.08/1.05`.

## Major Pity

- Major eligibility cadence: every 60 seconds.
- Chance bands: 0% before 60s, 25% from 60s, 40% from 180s, 60% from 300s.
- After two eligible misses, the next applicable major offer is forced.
- Pity counters reset on `BeginRun`.
- If no major can affect the current capped run state, telemetry records `no_applicable_major` and the offer falls back to stable/utility.

## Caps

- Run caps: damage `3.50`, fire rate `8.50`, max HP `36`, projectile count `9`, squad count `16`.
- Drawback/pressure caps: incoming damage max `1.75`, enemy pressure max `1.50`, enemy speed min `0.50`.
- Global technical projectile/squad cap `50` remains in runtime generation.
- Gate offers are preview-filtered at cap; runtime effect application also clamps actual stats.

## Save Isolation

- Benchmark runs suppress `RunStatsTracker` persistence, so wallet, best stats, and coin reward commits are not written.
- Benchmark start requests skip story/tutorial progression while the profile is active.
- Telemetry remains enabled in Editor/debug builds.

## DPS

Exporter output: `Tools/Balance/output/balance-v1.2.0-benchmark/benchmark_target_curve.csv`

| Checkpoint | Damage | Fire | HP | Projectile | Squad | Effective DPS |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| start | 1.90 | 6.40 | 20 | 6 | 12 | 178.676 |
| run caps | 3.50 | 8.50 | 36 | 9 | 16 | 509.529 |

## Telemetry

- Output path: `Application.persistentDataPath/BalanceTelemetry/balance-v1.2.0-benchmark/`
- Summary records `runMode`, `benchmarkProfileId`, and benchmark start stats.
- Gate events record resolved phase/effect/duration/drawback values.
- `major_roll` events record eligibility, chance, spawn/forced status, consecutive misses, and failure reason.

## Tests

- Unity compile check: 0 errors.
- New editor test methods executed in the Unity Editor domain by reflection: 4 pass.
- Full Unity EditMode Test Runner count: not run in this pass.
- Full Unity PlayMode Test Runner count: not run in this pass.

## Manual Playtest Checklist

- Start a benchmark run in Editor or Android development build.
- Confirm HUD/player starts at 1.90 / 6.40 / 20 / 6 / 12.
- Inspect gate labels at 0/120/240/360/480 seconds for resolved values.
- Confirm major gates appear only on 60 second cadence and pity after two eligible misses.
- Run past caps and confirm capped gates stop being offered.
- Confirm wallet/best/story/tutorial save state is unchanged after the benchmark death screen.
- Inspect telemetry JSONL/CSV for `benchmark` mode, phase values, and major pity events.

## Known Unverified Items

- Human survival target is not verified yet.
- Full Unity Test Runner EditMode/PlayMode suites were not executed because only Unity MCP was available and no direct Test Runner tool/CLI was found.
- Android development build was not produced in this pass.
