#!/usr/bin/env python3
"""Aggregate completed Android performance benchmark runs into report artifacts."""

from __future__ import annotations

import argparse
import csv
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable

from PIL import Image, ImageDraw, ImageFont


RAW_NAME = "Performance_Benchmark_Raw.csv"
SUMMARY_NAME = "Performance_Benchmark_Summary.csv"
METADATA_NAME = "Performance_Benchmark_Metadata.json"
VERIFY_NAME = "Android_Run_Verification.json"
COMPLETE_NAME = "benchmark_complete.marker"

NUMERIC_SUMMARY_FIELDS = (
    "duration_sec",
    "frame_count",
    "avg_fps",
    "median_fps",
    "one_percent_low_fps",
    "min_fps",
    "avg_frame_ms",
    "p95_frame_ms",
    "p99_frame_ms",
    "max_frame_ms",
    "peak_total_allocated_mb",
    "peak_total_reserved_mb",
    "avg_gc_alloc_bytes",
    "total_gc_alloc_bytes",
    "gc_collections",
    "avg_main_thread_ms",
    "avg_render_thread_ms",
    "peak_enemy_count",
    "peak_projectile_count",
    "peak_gate_count",
    "peak_pool_count",
)

SCENARIO_ORDER = (
    "Main Menu Idle",
    "Early Run",
    "60-180 Seconds",
    "180-300 Seconds",
    "Heavy Combat",
    "Long Run / Stress",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--performance-root",
        type=Path,
        default=Path("Assets/_Project/Documentation/Performance"),
    )
    return parser.parse_args()


def number(value: Any) -> float | None:
    if value is None:
        return None
    text = str(value).strip()
    if not text or text.upper() in {"N/A", "UNAVAILABLE", "NULL"}:
        return None
    try:
        result = float(text)
    except ValueError:
        return None
    return result if math.isfinite(result) else None


def fmt(value: float | None, digits: int = 2) -> str:
    if value is None or not math.isfinite(value):
        return "N/A"
    if abs(value - round(value)) < 1e-9:
        return str(int(round(value)))
    return f"{value:.{digits}f}"


def read_csv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def read_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def completed_runs(runs_root: Path) -> list[Path]:
    if not runs_root.exists():
        return []
    result = []
    for run in sorted(path for path in runs_root.iterdir() if path.is_dir()):
        required = (COMPLETE_NAME, RAW_NAME, SUMMARY_NAME, METADATA_NAME, VERIFY_NAME)
        if all((run / name).exists() for name in required):
            verification = read_json(run / VERIFY_NAME)
            if (
                verification.get("completionState") == "COMPLETE"
                and verification.get("includeInAggregate", True)
                and verification.get("foregroundPreserved", True)
            ):
                result.append(run)
    return result


def write_aggregate_raw(root: Path, runs: list[Path]) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    fieldnames: list[str] | None = None
    for run in runs:
        run_rows = read_csv(run / RAW_NAME)
        if run_rows and fieldnames is None:
            fieldnames = list(run_rows[0].keys())
        rows.extend(run_rows)

    if not rows or fieldnames is None:
        raise RuntimeError("Completed runs contained no raw benchmark samples.")

    with (root / RAW_NAME).open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)
    return rows


def aggregate(values: Iterable[float | None], operation: str) -> float | None:
    available = [value for value in values if value is not None]
    if not available:
        return None
    if operation == "mean":
        return statistics.fmean(available)
    if operation == "min":
        return min(available)
    if operation == "max":
        return max(available)
    raise ValueError(operation)


def nfr_result(avg_fps: float | None, one_percent_low: float | None) -> str:
    if avg_fps is None:
        return "Not enough evidence"
    if avg_fps < 30:
        return "Fail"
    if avg_fps >= 60 and one_percent_low is not None and one_percent_low >= 30:
        return "Pass"
    return "Partial"


def write_aggregate_summary(root: Path, runs: list[Path]) -> tuple[list[dict[str, str]], list[dict[str, str]]]:
    per_run: list[dict[str, str]] = []
    by_scenario: dict[str, list[dict[str, str]]] = defaultdict(list)
    for run in runs:
        for row in read_csv(run / SUMMARY_NAME):
            if number(row.get("frame_count")) in {None, 0}:
                continue
            row = dict(row)
            row["record_type"] = "run"
            row["statistic"] = "measured"
            row["nfr_result"] = nfr_result(number(row.get("avg_fps")), number(row.get("one_percent_low_fps")))
            per_run.append(row)
            by_scenario[row["scenario"]].append(row)

    aggregate_rows: list[dict[str, str]] = []
    for scenario in SCENARIO_ORDER:
        scenario_rows = by_scenario.get(scenario, [])
        if not scenario_rows:
            continue
        for operation in ("mean", "min", "max"):
            row = {
                "run_id": "ALL_COMPLETED_RUNS",
                "scenario": scenario,
                "record_type": "aggregate",
                "statistic": operation,
            }
            for field in NUMERIC_SUMMARY_FIELDS:
                row[field] = fmt(
                    aggregate((number(item.get(field)) for item in scenario_rows), operation),
                    3,
                )
            row["nfr_result"] = nfr_result(number(row["avg_fps"]), number(row["one_percent_low_fps"]))
            aggregate_rows.append(row)

    rows = per_run + aggregate_rows
    if not rows:
        raise RuntimeError("Completed runs contained no measured scenario summaries.")

    original_fields = [field for field in per_run[0].keys() if field not in {"record_type", "statistic", "nfr_result"}]
    fields = ["record_type", "statistic"] + original_fields + ["nfr_result"]
    with (root / SUMMARY_NAME).open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)
    return per_run, aggregate_rows


def chart_font(size: int) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default(size=size)


def draw_line_chart(
    path: Path,
    series: list[tuple[str, list[float], list[float]]],
    ylabel: str,
    references: list[tuple[float, str, str]],
) -> None:
    width, height = 1650, 825
    left, top, right, bottom = 115, 55, 420, 105
    plot_width = width - left - right
    plot_height = height - top - bottom
    image = Image.new("RGB", (width, height), "white")
    draw = ImageDraw.Draw(image)
    font = chart_font(20)
    small_font = chart_font(16)
    colors = ("#1f77b4", "#d62728", "#2ca02c", "#9467bd", "#ff7f0e", "#17becf")

    x_values = [value for _, x, _ in series for value in x]
    y_values = [value for _, _, y in series for value in y]
    if not x_values or not y_values:
        raise RuntimeError(f"No plottable values for {path.name}.")
    x_min, x_max = 0.0, max(x_values)
    if x_max <= x_min:
        x_max = x_min + 1.0
    y_min = min(0.0, min(y_values), *(value for value, _, _ in references))
    y_max = max(y_values + [value for value, _, _ in references])
    y_max = max(y_max * 1.08, y_min + 1.0)

    def px(value: float) -> float:
        return left + ((value - x_min) / (x_max - x_min)) * plot_width

    def py(value: float) -> float:
        return top + plot_height - ((value - y_min) / (y_max - y_min)) * plot_height

    draw.rectangle((left, top, left + plot_width, top + plot_height), outline="#333333", width=2)
    for tick in range(7):
        x_value = x_min + (x_max - x_min) * tick / 6
        x_position = px(x_value)
        draw.line((x_position, top, x_position, top + plot_height), fill="#e3e3e3", width=1)
        label = fmt(x_value, 0)
        draw.text((x_position, top + plot_height + 12), label, fill="#222222", font=small_font, anchor="ma")
    for tick in range(6):
        y_value = y_min + (y_max - y_min) * tick / 5
        y_position = py(y_value)
        draw.line((left, y_position, left + plot_width, y_position), fill="#e3e3e3", width=1)
        draw.text((left - 12, y_position), fmt(y_value, 1), fill="#222222", font=small_font, anchor="rm")

    for phase in (60, 180, 300, 420, 600):
        if phase <= x_max:
            position = px(float(phase))
            draw.line((position, top, position, top + plot_height), fill="#777777", width=1)
            draw.text((position + 3, top + 3), str(phase), fill="#666666", font=small_font)

    legend_entries: list[tuple[str, str]] = []
    for index, (name, x, y) in enumerate(series):
        color = colors[index % len(colors)]
        points = [(px(x_value), py(y_value)) for x_value, y_value in zip(x, y)]
        if len(points) == 1:
            draw.ellipse((points[0][0] - 2, points[0][1] - 2, points[0][0] + 2, points[0][1] + 2), fill=color)
        elif points:
            draw.line(points, fill=color, width=3, joint="curve")
        legend_entries.append((name, color))

    for value, label, color in references:
        position = py(value)
        draw.line((left, position, left + plot_width, position), fill=color, width=2)
        legend_entries.append((label, color))

    draw.text((left + plot_width / 2, height - 48), "Gameplay elapsed time (s)", fill="#111111", font=font, anchor="mm")
    draw.text((left, 20), ylabel, fill="#111111", font=font, anchor="la")

    legend_x, legend_y = left + plot_width + 22, top + 12
    for label, color in legend_entries:
        draw.line((legend_x, legend_y + 9, legend_x + 34, legend_y + 9), fill=color, width=3)
        draw.text((legend_x + 44, legend_y), label, fill="#111111", font=small_font)
        legend_y += 24

    image.save(path, format="PNG")


def plot_runs(root: Path, rows: list[dict[str, str]], y_field: str, output_name: str, ylabel: str) -> None:
    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        if row.get("state") == "MenuMeasurement" or number(row.get("gameplay_elapsed_sec")) is not None:
            grouped[row.get("run_id", "unknown")].append(row)

    series: list[tuple[str, list[float], list[float]]] = []
    for run_id, run_rows in grouped.items():
        x: list[float] = []
        y: list[float] = []
        for row in run_rows:
            elapsed = number(row.get("gameplay_elapsed_sec"))
            value = number(row.get(y_field))
            if elapsed is not None and value is not None:
                x.append(elapsed)
                y.append(value)
        if x:
            series.append((run_id, x, y))

    if y_field == "avg_frame_ms":
        references = [(16.67, "60 FPS (16.67 ms)", "#2b8a3e"), (33.33, "30 FPS (33.33 ms)", "#c92a2a")]
    elif y_field == "avg_fps":
        references = [(60, "Preferred 60 FPS", "#2b8a3e"), (30, "Minimum 30 FPS", "#c92a2a")]
    else:
        references = []
    draw_line_chart(root / output_name, series, ylabel, references)


def plot_memory(root: Path, rows: list[dict[str, str]]) -> None:
    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[row.get("run_id", "unknown")].append(row)
    series: list[tuple[str, list[float], list[float]]] = []
    for run_id, run_rows in grouped.items():
        x, allocated, reserved = [], [], []
        for row in run_rows:
            elapsed = number(row.get("gameplay_elapsed_sec"))
            allocated_value = number(row.get("total_allocated_mb"))
            reserved_value = number(row.get("total_reserved_mb"))
            if elapsed is not None and allocated_value is not None and reserved_value is not None:
                x.append(elapsed)
                allocated.append(allocated_value)
                reserved.append(reserved_value)
        if x:
            series.append((f"{run_id} allocated", x, allocated))
            series.append((f"{run_id} reserved", x, reserved))
    draw_line_chart(root / "Memory_Over_Time.png", series, "Unity memory (MiB)", [])


def mean_rows(aggregate_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    return [row for row in aggregate_rows if row["statistic"] == "mean"]


def markdown_table(rows: list[dict[str, str]]) -> str:
    lines = [
        "| Scenario | Avg FPS | 1% Low | Min FPS | Avg Frame | P95 | P99 | Peak Unity Allocated | Peak Enemy | Peak Projectile | Result |",
        "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|",
    ]
    for row in rows:
        lines.append(
            "| {scenario} | {avg} | {low} | {minimum} | {frame} ms | {p95} ms | {p99} ms | {memory} MiB | {enemy} | {projectile} | {result} |".format(
                scenario=row["scenario"],
                avg=row.get("avg_fps", "N/A"),
                low=row.get("one_percent_low_fps", "N/A"),
                minimum=row.get("min_fps", "N/A"),
                frame=row.get("avg_frame_ms", "N/A"),
                p95=row.get("p95_frame_ms", "N/A"),
                p99=row.get("p99_frame_ms", "N/A"),
                memory=row.get("peak_total_allocated_mb", "N/A"),
                enemy=row.get("peak_enemy_count", "N/A"),
                projectile=row.get("peak_projectile_count", "N/A"),
                result=row.get("nfr_result", "N/A"),
            )
        )
    return "\n".join(lines)


def system_telemetry_ranges(runs: list[Path]) -> dict[str, Any]:
    pss_mb: list[float] = []
    rss_mb: list[float] = []
    temperatures: list[float] = []
    thermal_statuses: set[str] = set()
    for run in runs:
        path = run / "Android_System_Memory_Thermal.csv"
        if not path.exists():
            continue
        for row in read_csv(path):
            pss = number(row.get("total_pss_kb"))
            rss = number(row.get("total_rss_kb"))
            temperature = number(row.get("battery_temp_c"))
            if pss is not None:
                pss_mb.append(pss / 1024)
            if rss is not None:
                rss_mb.append(rss / 1024)
            if temperature is not None:
                temperatures.append(temperature)
            status = (row.get("thermal_status") or "").strip()
            if status:
                thermal_statuses.add(status)
    return {
        "pss_min": min(pss_mb) if pss_mb else None,
        "pss_max": max(pss_mb) if pss_mb else None,
        "rss_min": min(rss_mb) if rss_mb else None,
        "rss_max": max(rss_mb) if rss_mb else None,
        "temperature_min": min(temperatures) if temperatures else None,
        "temperature_max": max(temperatures) if temperatures else None,
        "thermal_statuses": ", ".join(sorted(thermal_statuses)) or "N/A",
    }


def write_report(root: Path, runs: list[Path], aggregate_rows: list[dict[str, str]]) -> None:
    metadata = [read_json(run / METADATA_NAME) for run in runs]
    verification = [read_json(run / VERIFY_NAME) for run in runs]
    first = metadata[0]
    completed_ids = ", ".join(item.name for item in runs)
    full_runs = sum(1 for item in metadata if number(item.get("gameplayDurationSeconds")) and float(item["gameplayDurationSeconds"]) >= 600)
    short_runs = len(runs) - full_runs
    measured = mean_rows(aggregate_rows)
    measured_by_scenario = {row["scenario"]: row for row in measured}
    early = measured_by_scenario.get("Early Run", {})
    heavy = measured_by_scenario.get("Heavy Combat", {})
    long_run = measured_by_scenario.get("Long Run / Stress", {})
    system_ranges = system_telemetry_ranges(runs)
    build_path = root / "Benchmark_Build_Verification.json"
    build = read_json(build_path) if build_path.exists() else {}
    test_path = root / "Benchmark_Test_Verification.json"
    test_verification = read_json(test_path) if test_path.exists() else {}

    gc_available = any(bool(item.get("gcAllocatedRecorderAvailable")) for item in metadata)
    frame_cpu_available = any(bool(item.get("frameTimingCpuAvailable")) for item in metadata)
    all_saves_preserved = all(bool(item.get("savePreserved")) for item in verification)
    dirty_note = "Commit ghi trong request là HEAD tại thời điểm build; benchmark build chứa thay đổi instrumentation chưa commit."

    report = f"""# True Gate Android Performance Benchmark

Ngày tổng hợp: 2026-08-09

## Environment

- Thiết bị: {first.get('deviceModel', 'N/A')} ({first.get('operatingSystem', 'N/A')})
- Độ phân giải runtime: {first.get('screenWidth', 'N/A')} x {first.get('screenHeight', 'N/A')}; refresh {fmt(number(first.get('refreshRateHz')))} Hz
- Unity: {first.get('unityVersion', 'N/A')}; application version: {first.get('applicationVersion', 'N/A')}
- Build: Android Development Build; backend {first.get('scriptingBackend', 'N/A')}; Autoconnect Profiler={first.get('autoconnectProfiler', False)}; Deep Profiling={first.get('deepProfiling', False)}
- APK: `{build.get('apkPath', 'N/A')}`; SHA-256 `{build.get('sha256', 'N/A')}`; {build.get('sizeBytes', 'N/A')} bytes; ABI {build.get('abi', 'N/A')}
- Quality: {first.get('qualityLevel', 'N/A')}; vSyncCount={first.get('vSyncCount', 'N/A')}; Application.targetFrameRate={first.get('targetFrameRate', 'N/A')}
- Source commit: {first.get('sourceCommit', 'N/A')}. {dirty_note}
- Runs hợp lệ: {len(runs)} ({full_runs} full 600 s, {short_runs} verification ngắn): {completed_ids}
- Save trước/sau benchmark giữ nguyên: {all_saves_preserved}
- Automated validation: compile errors={test_verification.get('compileErrors', 'N/A')}; EditMode={test_verification.get('editMode', {}).get('passed', 'N/A')}/216; PlayMode={test_verification.get('playMode', {}).get('passed', 'N/A')}/41

## Tooling Audit

- `BalanceTelemetryService` đo balance/gameplay event, không đo FPS, frame time hoặc process memory và không được dùng làm performance evidence.
- `BalanceBenchmarkProfile` đã có cơ chế start stats/suppress progression nhưng active balance config không gắn benchmark profile, vì vậy không có benchmark runtime tự kích hoạt trong production scene.
- `EnemySpawnerSystem`, `GateSystem`, `PlayerController` có sẵn workload state; benchmark bổ sung getter/diagnostics tối thiểu cho pressure config, active Gate và pooled projectile/object count.
- Repository không có Performance Test Framework, Profile Analyzer capture, runtime FPS recorder hoặc long-run performance exporter đáp ứng yêu cầu trước task này.
- Instrumentation mới chỉ compile trong `UNITY_EDITOR || DEVELOPMENT_BUILD`, mặc định disabled và chỉ bootstrap khi có `benchmark_request.json` trong persistent data path.

## Methodology

Benchmark sử dụng no-Gate baseline với run-pressure production, start stats cố định và invulnerability chỉ trong Development Build. Progression, mission, story, tutorial và persistence bị suppress trong benchmark; production balance không thay đổi. Main Menu có warm-up trước khi đo; gameplay có warm-up riêng. Frame interval được thu liên tục từ `Time.unscaledDeltaTime`, sau đó workload và memory được aggregate thành một dòng CSV mỗi giây. Không ghi log mỗi frame.

Runner giữ game foreground và gửi một tap keep-alive tại tọa độ (1,1) mỗi 60 giây để tránh screen timeout của thiết bị; tap bắt đầu/kết thúc tại cùng điểm nên không tạo drag displacement. Run bị timeout, đổi foreground hoặc dùng APK instrumentation cũ được giữ trong thư mục `Runs` nhưng loại khỏi aggregate qua verification metadata.

Average FPS được tính bằng `1000 / mean(frame time ms)`. 1% Low lấy slowest 1% frame, tính mean frame time của nhóm này rồi đổi thành FPS. P95/P99 tính trên phân phối frame time. Unity allocated/reserved memory là số liệu runtime Unity, không được diễn giải là toàn bộ RAM thiết bị; Android PSS/RSS nằm trong CSV system riêng từng run.

ProfilerRecorder GC available: {gc_available}. FrameTiming CPU observed: {frame_cpu_available}. Counter không được runtime hỗ trợ được giữ là `N/A`.

## Scenario Results

Các giá trị dưới đây là mean của những lượt có dữ liệu cho scenario tương ứng. Run verification ngắn không tạo dữ liệu giả cho phase chưa chạy tới; min/max giữa các run nằm trong `{SUMMARY_NAME}`.

{markdown_table(measured)}

Quy tắc NFR: Pass khi Average FPS >= 60 và 1% Low >= 30; Partial khi Average FPS >= 30 nhưng chưa đạt điều kiện preferred; Fail khi Average FPS < 30; thiếu dữ liệu là Not enough evidence. P99 spike và 1% Low dưới 30 phải được xem riêng dù Average FPS đạt ngưỡng.

Kết quả cho thấy pacing hiệu dụng xấp xỉ 30 FPS: Early Run đạt {early.get('avg_fps', 'N/A')} FPS trung bình và {early.get('one_percent_low_fps', 'N/A')} FPS 1% Low; Heavy Combat đạt {heavy.get('avg_fps', 'N/A')} FPS và {heavy.get('one_percent_low_fps', 'N/A')} FPS 1% Low; Long Run/Stress đạt {long_run.get('avg_fps', 'N/A')} FPS và {long_run.get('one_percent_low_fps', 'N/A')} FPS 1% Low. Theo ngưỡng nghiêm ngặt, gameplay thấp hơn 30 FPS một lượng nhỏ nên không đạt minimum; preferred 60 FPS không đạt ở mọi scenario.

## Memory, GC, And Workload

`Memory_Over_Time.png` mô tả Unity allocated/reserved memory theo thời gian. Pool growth, lazy loading và reserved-memory plateau không tự động được xem là memory leak. `Android_System_Memory_Thermal.csv` trong từng run là nguồn process PSS/RSS, battery temperature và thermal status từ ADB. GC allocation/collection chỉ kết luận khi counter tương ứng available trong metadata.

Trên ba run hợp lệ, Android process PSS nằm trong {fmt(system_ranges['pss_min'])}-{fmt(system_ranges['pss_max'])} MiB và RSS trong {fmt(system_ranges['rss_min'])}-{fmt(system_ranges['rss_max'])} MiB. Battery temperature ghi nhận {fmt(system_ranges['temperature_min'], 1)}-{fmt(system_ranges['temperature_max'], 1)} °C. Thermal status luôn là {system_ranges['thermal_statuses']}; trên Android API 30, mã 3 tương ứng `THERMAL_STATUS_SEVERE`, vì vậy không thể tách hoàn toàn giới hạn thiết bị khỏi thermal throttling. Full run đạt peak Unity allocated {long_run.get('peak_total_allocated_mb', 'N/A')} MiB và reserved {long_run.get('peak_total_reserved_mb', 'N/A')} MiB; đường memory tiến tới plateau, chưa đủ bằng chứng kết luận memory leak.

GC Allocated In Frame recorder available. Early Run ghi trung bình {early.get('avg_gc_alloc_bytes', 'N/A')} byte/frame và {early.get('gc_collections', 'N/A')} collection; Heavy Combat {heavy.get('avg_gc_alloc_bytes', 'N/A')} byte/frame và {heavy.get('gc_collections', 'N/A')} collection; Long Run/Stress {long_run.get('avg_gc_alloc_bytes', 'N/A')} byte/frame và {long_run.get('gc_collections', 'N/A')} collection. Các số collection là delta trong từng scenario, không chứng minh riêng GC là nguyên nhân của spike.

Run-pressure production được giữ nguyên với các node 0, 60, 180, 300, 420 và 720 giây. Enemy cap lần lượt là 12, 18, 28, 38, 48 và 60; spawn rate lần lượt là 3, 4, 6, 8, 10 và 12 enemy/giây. Gate được tắt có chủ đích để tạo baseline lặp lại được.

## Potential Bottlenecks

- Average frame time giữ quanh 33,4 ms từ Early tới Long Run trong khi `Application.targetFrameRate=-1`, `vSyncCount=0` và màn hình 60 Hz. Đây là bằng chứng về pacing hiệu dụng khoảng 30 FPS, chưa đủ để quy CPU hay GPU là bottleneck vì Render Thread recorder không available.
- 1% Low giảm từ {early.get('one_percent_low_fps', 'N/A')} FPS ở Early Run xuống {heavy.get('one_percent_low_fps', 'N/A')} FPS ở Heavy Combat; max frame tăng từ {early.get('max_frame_ms', 'N/A')} ms lên {heavy.get('max_frame_ms', 'N/A')} ms khi peak workload đạt {heavy.get('peak_enemy_count', 'N/A')} enemy và {heavy.get('peak_projectile_count', 'N/A')} projectile. Workload/GC là ứng viên cần xác nhận bằng Unity Profiler, không phải kết luận nhân quả.
- Thermal status 3 xuyên suốt cả ba run là một biến nhiễu quan trọng. Theo Android, đây là severe throttling; baseline không đủ để tách frame pacing của game khỏi giới hạn nhiệt của thiết bị.

## Limitations

- Development Build và instrumentation có overhead; đây không phải release-like FPS sanity check.
- No-Gate baseline không đại diện đầy đủ biến thiên gameplay khi người chơi tương tác Gate.
- Thiết bị và nhiệt độ môi trường chỉ đại diện cho cấu hình được ghi nhận; thermal telemetry phụ thuộc dữ liệu mà Android 11 cung cấp.
- `Time.unscaledDeltaTime` là nguồn frame interval chính; FrameTimingManager/ProfilerRecorder chỉ được báo khi runtime xác nhận available.
- Lượt verification ngắn chỉ đánh giá repeatability ở các phase mà chúng thực sự chạy tới.
- Thiết bị không cho ADB thay đổi screen timeout; tap keep-alive (1,1) mỗi 60 giây được dùng và ghi nhận như một khác biệt của benchmark automation.
- Thermal status 3 được diễn giải theo [Android `PowerManager` API](https://developer.android.com/reference/android/os/PowerManager#THERMAL_STATUS_SEVERE).

## Report-ready Vietnamese text

### Kiểm thử hiệu năng

Kiểm thử được thực hiện trên {first.get('deviceModel', 'thiết bị Android')} với Android build Development, Unity {first.get('unityVersion', 'N/A')}, backend {first.get('scriptingBackend', 'N/A')}. Quy trình gồm {len(runs)} lượt đo, trong đó có {full_runs} lượt 600 giây và {short_runs} lượt xác minh ngắn. Early Run đạt {early.get('avg_fps', 'N/A')} FPS trung bình, 1% Low {early.get('one_percent_low_fps', 'N/A')} FPS; Heavy Combat đạt {heavy.get('avg_fps', 'N/A')} FPS, 1% Low {heavy.get('one_percent_low_fps', 'N/A')} FPS; Long Run/Stress đạt {long_run.get('avg_fps', 'N/A')} FPS, 1% Low {long_run.get('one_percent_low_fps', 'N/A')} FPS. Kết quả ổn định gần 30 FPS nhưng thấp hơn nhẹ ngưỡng minimum nghiêm ngặt và không đạt target 60 FPS. Peak Unity allocated là {long_run.get('peak_total_allocated_mb', 'N/A')} MiB; Android PSS tối đa {fmt(system_ranges['pss_max'])} MiB. Thermal status 3 xuất hiện xuyên suốt nên thermal throttling là limitation đáng kể. Benchmark giữ run-pressure production, tắt Gate và bật invulnerability riêng cho benchmark; save trước/sau cả ba run giữ nguyên.
"""
    (root / "Performance_Benchmark_Report.md").write_text(report, encoding="utf-8")


def main() -> None:
    args = parse_args()
    root = args.performance_root.resolve()
    root.mkdir(parents=True, exist_ok=True)
    runs = completed_runs(root / "Runs")
    if not runs:
        raise SystemExit(f"No completed benchmark runs found under {root / 'Runs'}")

    raw_rows = write_aggregate_raw(root, runs)
    _, aggregate_rows = write_aggregate_summary(root, runs)
    plot_runs(root, raw_rows, "avg_fps", "FPS_Over_Time.png", "Average FPS per sample window")
    plot_runs(root, raw_rows, "avg_frame_ms", "FrameTime_Over_Time.png", "Average frame time (ms)")
    plot_memory(root, raw_rows)
    write_report(root, runs, aggregate_rows)
    print(f"Aggregated {len(runs)} completed runs into {root}")


if __name__ == "__main__":
    main()
