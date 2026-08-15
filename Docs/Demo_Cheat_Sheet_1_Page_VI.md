# True Gate — Phao demo 1 trang

## Pitch 20 giây

> True Gate là game mobile 2D portrait survival auto-shooter. Người chơi kéo UNIT-07 theo trục ngang, squad tự bắn và cứ khoảng 15 giây chọn một trong ba Gate. Gate tạo quyết định trong run; coin, upgrade, mission và story tạo vòng lặp qua nhiều run.

## 7 phút

1. **0:00** Pitch + Main Menu.
2. **0:45** Start → move → auto-fire → enemy → chọn Gate.
3. **2:30** Pause/Settings → Game Over snapshot.
4. **3:15** Upgrade → Mission → story/save.
5. **4:30** Sơ đồ runtime.
6. **5:15** Code: `StartRunInternal` → `HandleSquadDefeated` → Gate → Stats → Save → Pool.
7. **6:30** Active CSV + tests + benchmark + giới hạn.

## Luồng một câu

`UI request → GameManager → Player/Enemy/Gate → RunStats snapshot → Mission/Story/Save → Game Over/Upgrade → run mới`.

## Code tabs

- `GameManager.cs`: start/pause/end/orchestrate.
- `GameStateMachine.cs`: Bootstrap, MainMenu, Playing, Cutscene, Paused, GameOver.
- `GateSystem.cs` + `GateConfig.cs`: offer/chosen/effect.
- `BulletSpawner.cs` + `PoolSystem.cs`: fire + reuse object.
- `RunStatsTracker.cs`: time/kill/coin/score/snapshot.
- `MissionProgressEvaluator.cs`: absolute/delta/best run.
- `SaveService.cs` + `LocalSaveRepository.cs`: schema + temp/backup/main.
- `StoryCutsceneUnlockRules.cs`: prerequisite + loop/time/kill.

## Dataset

> Không có training dataset vì không train ML. Có: (1) ScriptableObject config, (2) CSV/JSON do balance simulator/exporter sinh, (3) local player save, (4) benchmark telemetry.

Dùng thư mục active:

`Tools/Balance/output/balance-v1.4.1-meta-stat-progression/`

Không demo CSV v1.0 nằm ngay ở root `output/`.

## Số liệu có thể nói

- Unity `6000.4.2f1`, Android portrait, IL2CPP ARM64.
- Build version `0.1.1`, code 2, min Android API 25.
- 47 missions; 7 mốc story chính; save schema 11.
- Test artifact benchmark 09/08/2026: 216 EditMode + 41 PlayMode pass.
- OPPO CPH2059: gameplay trung bình khoảng 29.9 FPS; 1% low khoảng 22.4–25.1; peak Unity allocated khoảng 146.5 MiB.

## Bốn câu gài

**GameManager có to không?** Có; hiện là coordinator/composition root, logic cục bộ đã tách. Production sẽ tách bootstrap, run orchestration và story routing.

**Cloud save?** Có interface/merge flow, nhưng provider runtime là no-op; chưa phải cloud production.

**Test pass = coverage?** Không. Chỉ chứng minh các assertion đã đăng ký; không thay code coverage/device/performance test.

**60 FPS?** Chưa đạt. Benchmark gần 30 FPS và còn thermal/device/development-build limitations.

## Công thức trả lời code

`Kết luận → class/method → dữ liệu vào/ra → trade-off/giới hạn`.

Nếu không nhớ chi tiết:

> Phần em chắc chắn là trách nhiệm của module và luồng dữ liệu như trên. Con số/implementation detail đó em xin kiểm tra trực tiếp trong config/code để không trả lời sai.

## Plan B

Live hỏng → thử lại một lần trong 20 giây → chuyển screenshot → sơ đồ → code → evidence. Không debug dài trước lớp.

