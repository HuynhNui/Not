# True Gate Balance Simulator

1. In Unity, run `Tools > Balance > Export True Gate V1 Config`.
2. Run:

```powershell
python Tools/Balance/balance_simulator_true_gate_v1.py
```

The exporter reads Unity balance assets and default config APIs, then writes
`Tools/Balance/output/true_gate_balance_v1.json`.

The simulator generates:

- `meta_progression.csv`
- `pressure_curve.csv`
- `gate_schedule.csv`
- `economy_projection_basic_only.csv`
- `simulator_summary.json`

The economy projection is a sensitivity estimate based on Basic enemy reward
and sampled spawn pressure. Use local telemetry distributions for production
tuning.
