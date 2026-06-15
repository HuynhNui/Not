#!/usr/bin/env python3
"""Generate readable balance projections from Unity-exported True Gate v1 data."""

from __future__ import annotations

import argparse
import csv
import json
import math
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = SCRIPT_DIR / "output" / "true_gate_balance_v1.json"
DEFAULT_OUTPUT = SCRIPT_DIR / "output"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    return parser.parse_args()


def write_csv(path: Path, fieldnames: list[str], rows: list[dict]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def build_meta_rows(data: dict, projected_coins: dict[int, float]) -> list[dict]:
    rows = []
    cumulative_cost = 0

    for level in data["metaLevels"]:
        cumulative_cost += level["cost"]
        row = dict(level)
        row["cumulativeCost"] = cumulative_cost

        for minutes, coins in projected_coins.items():
            row[f"runsAt{minutes}Min"] = (
                math.ceil(cumulative_cost / coins) if coins > 0 else 0
            )

        rows.append(row)

    return rows


def build_economy_projection(data: dict) -> tuple[list[dict], dict[int, float]]:
    basic_reward = next(
        role["rewardPoints"]
        for role in data["enemyRoles"]
        if role["role"] == "Basic"
    )
    reward_scale = data["economy"]["rewardScale"]
    rows = []
    run_totals: dict[int, float] = {}
    cumulative = 0.0
    previous_seconds = 0

    for sample in data["pressureSamples"]:
        seconds = sample["seconds"]
        if seconds == 0:
            continue

        interval_seconds = seconds - previous_seconds
        previous_seconds = seconds
        reward = (
            sample["spawnPerSecond"]
            * interval_seconds
            * basic_reward
            * reward_scale
        )
        cumulative += reward
        rows.append(
            {
                "seconds": seconds,
                "spawnPerSecond": sample["spawnPerSecond"],
                "basicRewardPoints": basic_reward,
                "projectedIntervalCoins": round(reward, 3),
                "projectedCumulativeCoins": round(cumulative, 3),
            }
        )

        if seconds in (120, 300, 600):
            run_totals[seconds // 60] = round(cumulative, 3)

    return rows, run_totals


def main() -> int:
    args = parse_args()
    with args.config.open(encoding="utf-8") as handle:
        data = json.load(handle)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    economy_rows, run_totals = build_economy_projection(data)
    meta_rows = build_meta_rows(data, run_totals)

    write_csv(
        args.output_dir / "meta_progression.csv",
        list(meta_rows[0].keys()),
        meta_rows,
    )
    write_csv(
        args.output_dir / "pressure_curve.csv",
        list(data["pressureSamples"][0].keys()),
        data["pressureSamples"],
    )
    write_csv(
        args.output_dir / "gate_schedule.csv",
        list(data["gateSchedule"][0].keys()),
        data["gateSchedule"],
    )
    write_csv(
        args.output_dir / "economy_projection_basic_only.csv",
        list(economy_rows[0].keys()),
        economy_rows,
    )

    summary = {
        "balanceVersion": data["balanceVersion"],
        "metaLevels": len(data["metaLevels"]),
        "pressureSamples": len(data["pressureSamples"]),
        "gateSets": len(data["gateSchedule"]),
        "projectedBasicOnlyCoinsByRunMinutes": run_totals,
        "fullMetaCumulativeCost": meta_rows[-1]["cumulativeCost"],
        "fullMetaRunsByDuration": {
            key: value
            for key, value in meta_rows[-1].items()
            if key.startswith("runsAt")
        },
        "note": (
            "Economy projection is a basic-enemy-only sensitivity estimate. "
            "Use telemetry distributions for production tuning."
        ),
    }
    (args.output_dir / "simulator_summary.json").write_text(
        json.dumps(summary, indent=2),
        encoding="utf-8",
    )

    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
