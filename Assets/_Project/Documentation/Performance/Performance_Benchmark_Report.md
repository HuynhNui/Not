# True Gate Android Performance Benchmark

Ngày tổng hợp: 2026-08-09

## Environment

- Thiết bị: OPPO CPH2059 (Android OS 11 / API-30 (RKQ1.200903.002/1736173128811))
- Độ phân giải runtime: 1080 x 2400; refresh 60 Hz
- Unity: 6000.4.2f1; application version: 0.1.1
- Build: Android Development Build; backend IL2CPP; Autoconnect Profiler=False; Deep Profiling=False
- APK: `Builds/Android/TrueGate-0.1.1-performance-dev.apk`; SHA-256 `b73d2308bd17a49f3008c86e5f6bf4793367ede3903c5f0c6cfb8b263df34dd5`; 109203843 bytes; ABI arm64-v8a
- Quality: Very Low; vSyncCount=0; Application.targetFrameRate=-1
- Source commit: d6954fe04761b6aff959fdd35412ea88478ad776. Commit ghi trong request là HEAD tại thời điểm build; benchmark build chứa thay đổi instrumentation chưa commit.
- Runs hợp lệ: 3 (1 full 600 s, 2 verification ngắn): oppo_full_03_20260809, oppo_verify_02_20260809, oppo_verify_03_20260809
- Save trước/sau benchmark giữ nguyên: True
- Automated validation: compile errors=0; EditMode=216/216; PlayMode=41/41

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

ProfilerRecorder GC available: True. FrameTiming CPU observed: True. Counter không được runtime hỗ trợ được giữ là `N/A`.

## Scenario Results

Các giá trị dưới đây là mean của những lượt có dữ liệu cho scenario tương ứng. Run verification ngắn không tạo dữ liệu giả cho phase chưa chạy tới; min/max giữa các run nằm trong `Performance_Benchmark_Summary.csv`.

| Scenario | Avg FPS | 1% Low | Min FPS | Avg Frame | P95 | P99 | Peak Unity Allocated | Peak Enemy | Peak Projectile | Result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Main Menu Idle | 30.000 | 29.782 | 29.634 | 33.333 ms | 33.429 ms | 33.489 ms | 141.773 MiB | 0 | 0 | Partial |
| Early Run | 29.943 | 25.106 | 19.961 | 33.396 ms | 33.483 ms | 33.603 ms | 145.129 MiB | 13.667 | 76 | Fail |
| 60-180 Seconds | 29.924 | 23.837 | 18.309 | 33.418 ms | 33.464 ms | 33.540 ms | 145.792 MiB | 19.667 | 64 | Fail |
| 180-300 Seconds | 29.908 | 22.888 | 14.987 | 33.435 ms | 33.447 ms | 33.535 ms | 146.249 MiB | 30 | 46 | Fail |
| Heavy Combat | 29.900 | 22.437 | 14.995 | 33.445 ms | 33.456 ms | 33.545 ms | 146.435 MiB | 38 | 39 | Fail |
| Long Run / Stress | 29.908 | 22.907 | 14.979 | 33.435 ms | 33.455 ms | 33.541 ms | 146.533 MiB | 44 | 40 | Fail |

Quy tắc NFR: Pass khi Average FPS >= 60 và 1% Low >= 30; Partial khi Average FPS >= 30 nhưng chưa đạt điều kiện preferred; Fail khi Average FPS < 30; thiếu dữ liệu là Not enough evidence. P99 spike và 1% Low dưới 30 phải được xem riêng dù Average FPS đạt ngưỡng.

Kết quả cho thấy pacing hiệu dụng xấp xỉ 30 FPS: Early Run đạt 29.943 FPS trung bình và 25.106 FPS 1% Low; Heavy Combat đạt 29.900 FPS và 22.437 FPS 1% Low; Long Run/Stress đạt 29.908 FPS và 22.907 FPS 1% Low. Theo ngưỡng nghiêm ngặt, gameplay thấp hơn 30 FPS một lượng nhỏ nên không đạt minimum; preferred 60 FPS không đạt ở mọi scenario.

## Memory, GC, And Workload

`Memory_Over_Time.png` mô tả Unity allocated/reserved memory theo thời gian. Pool growth, lazy loading và reserved-memory plateau không tự động được xem là memory leak. `Android_System_Memory_Thermal.csv` trong từng run là nguồn process PSS/RSS, battery temperature và thermal status từ ADB. GC allocation/collection chỉ kết luận khi counter tương ứng available trong metadata.

Trên ba run hợp lệ, Android process PSS nằm trong 381.77-450.13 MiB và RSS trong 483.52-553.83 MiB. Battery temperature ghi nhận 32.6-39 °C. Thermal status luôn là 3; trên Android API 30, mã 3 tương ứng `THERMAL_STATUS_SEVERE`, vì vậy không thể tách hoàn toàn giới hạn thiết bị khỏi thermal throttling. Full run đạt peak Unity allocated 146.533 MiB và reserved 277.793 MiB; đường memory tiến tới plateau, chưa đủ bằng chứng kết luận memory leak.

GC Allocated In Frame recorder available. Early Run ghi trung bình 2152.459 byte/frame và 6 collection; Heavy Combat 2144.903 byte/frame và 15 collection; Long Run/Stress 1983.327 byte/frame và 21 collection. Các số collection là delta trong từng scenario, không chứng minh riêng GC là nguyên nhân của spike.

Run-pressure production được giữ nguyên với các node 0, 60, 180, 300, 420 và 720 giây. Enemy cap lần lượt là 12, 18, 28, 38, 48 và 60; spawn rate lần lượt là 3, 4, 6, 8, 10 và 12 enemy/giây. Gate được tắt có chủ đích để tạo baseline lặp lại được.

## Potential Bottlenecks

- Average frame time giữ quanh 33,4 ms từ Early tới Long Run trong khi `Application.targetFrameRate=-1`, `vSyncCount=0` và màn hình 60 Hz. Đây là bằng chứng về pacing hiệu dụng khoảng 30 FPS, chưa đủ để quy CPU hay GPU là bottleneck vì Render Thread recorder không available.
- 1% Low giảm từ 25.106 FPS ở Early Run xuống 22.437 FPS ở Heavy Combat; max frame tăng từ 50.097 ms lên 66.687 ms khi peak workload đạt 38 enemy và 39 projectile. Workload/GC là ứng viên cần xác nhận bằng Unity Profiler, không phải kết luận nhân quả.
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

Kiểm thử được thực hiện trên OPPO CPH2059 với Android build Development, Unity 6000.4.2f1, backend IL2CPP. Quy trình gồm 3 lượt đo, trong đó có 1 lượt 600 giây và 2 lượt xác minh ngắn. Early Run đạt 29.943 FPS trung bình, 1% Low 25.106 FPS; Heavy Combat đạt 29.900 FPS, 1% Low 22.437 FPS; Long Run/Stress đạt 29.908 FPS, 1% Low 22.907 FPS. Kết quả ổn định gần 30 FPS nhưng thấp hơn nhẹ ngưỡng minimum nghiêm ngặt và không đạt target 60 FPS. Peak Unity allocated là 146.533 MiB; Android PSS tối đa 450.13 MiB. Thermal status 3 xuất hiện xuyên suốt nên thermal throttling là limitation đáng kể. Benchmark giữ run-pressure production, tắt Gate và bật invulnerability riêng cho benchmark; save trước/sau cả ba run giữ nguyên.
