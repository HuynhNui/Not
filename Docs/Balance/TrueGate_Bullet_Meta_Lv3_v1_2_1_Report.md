# TrueGate Bullet Meta Lv3 v1.2.1 Report

## Summary

- Active benchmark version is now `balance-v1.2.1-benchmark`.
- `BULLET` permanent meta max level is `3`.
- `DMG`, `FIRE`, `HP`, and `PLAYER` remain max level `5`.
- Unsupported `MoveSpeed` returns max level `0`.
- Bullet power is preserved: Lv0/Lv1/Lv2/Lv3 = `1/2/4/6`.
- Compatibility rows 4/5 plateau at projectile value `6`, but are not purchasable.

## Save Migration

- `SaveData.CurrentSchemaVersion` is now `7`.
- Schema 6 ProjectileCount Lv4/Lv5 migrates to Lv3.
- Damage/fire/HP/player levels, wallet, story, tutorial, and best stats are preserved.
- Development log emits: `[META MIGRATION] ProjectileCount level 5 -> 3; max projectile value preserved at 6.`

## Assets

- New meta: `Assets/_Project/Data/Balance/V1_2_Benchmark/PlayerMetaBalanceConfig_v1_2_1_Benchmark.asset`
- New telemetry: `Assets/_Project/Data/Balance/V1_2_Benchmark/BalanceTelemetryConfig_v1_2_1_benchmark.asset`
- New bootstrap: `Assets/_Project/Data/Balance/V1_2_Benchmark/BalanceBootstrapConfig_v1_2_1_benchmark.asset`
- Existing benchmark profile kept and updated to profile id `full-meta-strongest-player-v2-bullet-lv3`.
- `Main.unity` GameManager now references the v1.2.1 bootstrap.

## Benchmark

Benchmark start remains:

| Damage | Fire | HP | Projectile | Squad | DPS |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1.90 | 6.40 | 20 | 6 | 12 | 178.676 |

Run cap remains projectile `9`; gate scaling was not changed.

## Debug Coin Button

- Added `DebugAddCoinsButton` under `SettingsPanel/MainPanel`.
- Label: `+10K COIN`.
- It calls `SaveService.TryAddDebugWalletCoins(10000)`.
- Button is only active in Editor/debug builds.

## Export

Exporter now includes per-upgrade meta tracks with `maxLevel` and purchasable values.

Output:

- `Tools/Balance/output/balance-v1.2.1-benchmark/true_gate_balance_v1_2_1_benchmark.json`
- `Tools/Balance/output/balance-v1.2.1-benchmark/gate_phase_values.csv`
- `Tools/Balance/output/balance-v1.2.1-benchmark/benchmark_target_curve.csv`

## Verification

- Unity compile check: 0 errors.
- Editor-domain tests run by reflection: 62 pass.
- Runtime-style `BalanceRuntimePlayModeTests` enumerators run in Editor domain: 10 pass.
- Full Unity Test Runner UI/CLI was not available through the current MCP tools, so official EditMode/PlayMode runner counts were not produced.

## Unchanged

- Enemy pressure and enemy stats.
- Gate phase values, major chance, and pity logic.
- Economy and cost tuning except Bullet purchasable max.
- Bullet sprites/prefabs.
- `Assets/Front/Upheaval_TMP.asset`.
