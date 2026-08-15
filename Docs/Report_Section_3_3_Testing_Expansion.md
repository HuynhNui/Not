# Mở rộng mục 3.3 — Kiểm thử và phân tích kết quả

> **Phạm vi audit:** repository True Gate tại commit `d6954fe04761b6aff959fdd35412ea88478ad776`, ngày 08/08/2026.  
> **Nguyên tắc kết luận:** Requirement → Implementation → Test/Verification → Assertion/Evidence → Conclusion.  
> **Lưu ý về trạng thái:** hai tệp `Assets/Front/Upheaval_TMP.asset` và `ProjectSettings/ProjectSettings.asset` đã có thay đổi cục bộ trước audit; audit không sửa hai tệp này. Các test được chạy trên working tree hiện tại. Build Android không được tạo lại trong audit này.

## A. Audit findings

### A.1. Phạm vi và nguồn đã kiểm tra

Audit đã đọc toàn bộ 31 tệp chứa NUnit/Unity Test Framework test trong hai thư mục `Assets/_Project/Tests/Editor/` và `Assets/_Project/Tests/PlayMode/`; không tìm thấy tệp có `[Test]`, `[TestCase]` hoặc `[UnityTest]` ở vị trí khác dưới `Assets/`. Ngoài source test, các nguồn được đối chiếu gồm:

- `Assets/_Project/Documentation/QA/2026-07-31_Beta_0.1.1_QA_Rerun.md` và baseline lỗi trước đó trong `2026-07-31_Test_Run.md`;
- toàn bộ tài liệu `Assets/_Project/Documentation/MobileReadiness/`, đặc biệt `09_Build_Verification.md`;
- implementation của `GameManager`, `RunStatsTracker`, `SaveService`, `SaveData`, `LocalSaveRepository`, `PlayerMetaUpgradeService`, `StoryCutsceneUnlockRules`, `StoryCutsceneRuntimeController`, `MissionSystem` và `MissionCatalog`;
- các báo cáo và dữ liệu balance trong `Docs/Balance/` và `Tools/Balance/output/`;
- cấu hình Unity trong `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/ProjectSettings.asset`, `Packages/manifest.json` và assembly test PlayMode.

Project dùng Unity `6000.4.2f1`, Unity Test Framework `1.6.0`, URP `17.4.0`, Input System `1.8.0`. PlayMode test nằm trong assembly `TrueGate.PlayModeTests`; EditMode test được biên dịch trong ngữ cảnh Editor.

### A.2. Baseline test được xác nhận tại working tree hiện tại

Các số liệu 213 EditMode và 41 PlayMode trong QA report ngày 31/07/2026 vẫn đúng, nhưng audit không chỉ kế thừa con số cũ. Hai suite đã được chạy lại trực tiếp trong Unity Editor tại commit hiện tại:

| Suite | Job hiện tại | Tổng | Passed | Failed | Skipped | Thời gian |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| EditMode | `6867ee0b474a` | 213 | 213 | 0 | 0 | 11,22 giây |
| PlayMode | `42efcc9f4851` | 41 | 41 | 0 | 0 | 1,91 giây |
| **Tổng** | — | **254** | **254** | **0** | **0** | — |

Unity Compilation Pipeline đồng thời báo `0` C# compile error và Editor không ở trạng thái compiling sau khi chạy test. Như vậy, so với `2026-07-31_Beta_0.1.1_QA_Rerun.md`, tổng số test và kết quả không thay đổi. Điểm khác là bảng trên là bằng chứng rerun ngày 08/08/2026 trên commit `d6954fe`, trong khi QA report cũ ghi tested source commit `099b36a`.

### A.3. Inventory test fixture

Có 24 tệp/fixture EditMode và 7 tệp/fixture PlayMode. Số test do Unity Test Runner phát hiện theo fixture như sau:

| EditMode fixture | Test | EditMode fixture | Test |
| --- | ---: | --- | ---: |
| `AudioSystemAssetTests` | 7 | `BalanceV1MathTests` | 56 |
| `DialogueShuffleBagTests` | 2 | `EnemyPrefabAuthoringTests` | 5 |
| `GameplayDialogueContentTests` | 2 | `GameplayDialogueResolverTests` | 9 |
| `GameplayDialogueSchedulerTests` | 5 | `GameplayVoiceEmotionResolverTests` | 16 |
| `GateScalingProfileTests` | 9 | `MissionRuntimePhaseETests` | 15 |
| `MissionSavePhaseDTests` | 6 | `MissionSystemPhaseCTests` | 7 |
| `MissionUiPhaseHTests` | 10 | `PlayerMetaUpgradeTrackTests` | 10 |
| `SafeAreaFitterTests` | 7 | `StoryVoiceCatalogTests` | 6 |
| `TutorialCutsceneAndGateTests` | 3 | `TutorialFlowDecisionTests` | 5 |
| `TutorialSaveTests` | 6 | `UpdateOnboardingTests` | 9 |
| `VoiceIdUtilityTests` | 8 | `VoiceManifestCollectorTests` | 5 |
| `VoiceManifestCsvWriterTests` | 2 | `VoiceManifestValidationTests` | 3 |
| **Tổng EditMode** | **213** |  |  |

| PlayMode fixture | Test |
| --- | ---: |
| `BalanceRuntimePlayModeTests` | 10 |
| `EnemyContactAndProjectilePlayModeTests` | 13 |
| `GameplayDialoguePlayModeTests` | 1 |
| `HitboxRegressionPlayModeTests` | 3 |
| `SafeAreaHierarchyPlayModeTests` | 1 |
| `StoryVoicePlaybackPlayModeTests` | 9 |
| `TutorialOverlayPlayModeTests` | 4 |
| **Tổng PlayMode** | **41** |

Để tránh đếm trùng các fixture có tính chất liên ngành, có thể phân hoạch 254 test theo nhóm fixture lớn: balance/core/gate/meta 85; enemy–projectile–hitbox 21; mission 38; tutorial/onboarding 27; story–dialogue–voice–audio 75; safe-area/mobile UI 8. Phân hoạch này chỉ dùng để giải thích cấu trúc suite; bảng evidence bên dưới mới thể hiện chi tiết subsystem và không ngụ ý code coverage phần trăm.

### A.4. Các phát hiện trọng yếu

1. **Final Choice được đánh giá sau khi số run đã tăng.** `GameManager.HandleSquadDefeated()` gọi `RunStatsTracker.EndRun()` trước khi tạo snapshot và gọi story runtime. `EndRun()` gọi `SaveService.RecordRunResult()`, tại đó `totalRunsCompleted` tăng đúng một lần. `StoryCutsceneRuntimeController.TryPlayPostRunCutscene()` sau đó lấy chính giá trị đã cập nhật từ save để tạo context. Vì vậy run làm giá trị chuyển từ 49 sang 50 có thể mở Final Choice.
2. **Final Choice không có upper bound thực tế.** Rule đặt `MinLoopCount = 50`, `MaxLoopCount = int.MaxValue`, survival tối thiểu 420 giây, prerequisite `CS_06_SystemFatigue`, và yêu cầu `CS_07_FinalChoice_PreChoice` chưa được ghi nhận. Test khóa các giá trị 49, 50, 51, 100 và 419,999/420 giây; test cũng khóa missing prerequisite, alias normalization và replay prevention.
3. **Final Choice → mission cuối → Terminal Protocol có regression evidence cụ thể.** Khi một branch Final Choice hoàn tất, runtime ghi cutscene, gọi `MissionSystem.NotifyFinalChoiceResolved()`, đặt `finalChoiceResolved`, hoàn tất `break_final_choice`, sau đó mở hai head mission Terminal đầu tiên (`terminal_1000_kills_run`, `terminal_10000_total_kills`) nhưng giữ các mission tiếp theo của từng category ở trạng thái khóa. Test `FinalChoiceResolved_CompletesFinalMission` kiểm tra trực tiếp các assertion này.
4. **Meta progression có bằng chứng tốt về cap và migration.** Damage/Fire Rate/Max HP có max level 5; Projectile Count max level 2, tương ứng giá trị 1/2/3 projectile; Squad Size max level 3, tương ứng 1/2/3/4 unit. Test kiểm tra purchase Projectile dừng tại level 2, schema 7 clamp các level cũ 5 xuống 2/3 mà giữ Damage, Coin, story và tutorial, đồng thời kiểm tra `ApplyStatsToPlayer()` đồng bộ follower.
5. **Save có implementation bảo vệ nhưng test không bao phủ đầy đủ mọi failure mode.** `LocalSaveRepository` ghi `save.tmp`, sao chép file chính cũ sang `save.bak`, rồi thay file chính; load thử `save.json` trước và fallback `save.bak`. Tuy nhiên suite hiện không có test tự động làm hỏng `save.json` rồi xác nhận fallback, cũng chưa có test khởi tạo một `SaveService` thứ hai để chứng minh round-trip reload toàn bộ mission state. Không được biến implementation này thành kết luận “đã kiểm thử đầy đủ”.
6. **Không có performance profiling artifact đủ tin cậy.** Không tìm thấy Unity Profiler capture, Memory Profiler snapshot, FPS/frame-time series, GC Alloc/frame, peak RAM, CPU/GPU trace, Android profiling log hoặc long-run performance soak được lưu trong repo. Balance telemetry và simulator output đo hành vi/balance (gate, pressure, DPS ước tính), không phải runtime performance telemetry.
7. **Build Beta 0.1.1 có evidence package/device rõ nhưng chỉ trên một thiết bị.** APK `TrueGate-Beta-0.1.1-vc2.apk` vẫn tồn tại với đúng kích thước 69.737.769 byte và SHA-256 khớp build report. Có một APK `TrueGate_0.1.1_NewIcon.apk` mới hơn theo timestamp, nhưng không có build-verification report gắn source commit/package inspection/device smoke tương ứng; vì vậy audit không dùng artifact này để mở rộng kết luận.

## B. Testing evidence map

| Area | Implementation | Test/Evidence | Key behavior | Result |
| --- | --- | --- | --- | --- |
| Run lifecycle và commit kết quả | `GameManager.HandleSquadDefeated`, `RunStatsTracker.EndRun`, `SaveService.RecordRunResult` | `BalanceV1MathTests.RecordRunResult_IncrementsCompletedRunsWithoutNewBest`; `BalanceRuntimePlayModeTests.WalletCoins_CommitOnlyWhenRunEnds` | Kết thúc run một lần; cập nhật run count, kill, best stat và wallet ở thời điểm kết thúc, không commit Coin đang chờ giữa run | **Pass** trong suite hiện tại |
| Player, squad và combat | `PlayerController`, `MainPlayerUnit`, `FollowerUnit`, `BulletSpawner`, combat scaling | `BalanceV1MathTests`; `PlayerMetaUpgradeTrackTests.ApplyStatsToPlayer_EliteSquadFollowersMirrorMainStats`; `BalanceRuntimePlayModeTests` | Damage, fire rate, HP, projectile/squad, recruit/promotion HP ratio, idempotent stat application và volley count | **Pass** |
| Enemy, projectile và hitbox | `EnemyController`, projectile, overlap/contact handling, Chomboom | `EnemyPrefabAuthoringTests` (5); `EnemyContactAndProjectilePlayModeTests` (13); `HitboxRegressionPlayModeTests` (3) | Collider thủ công không bị thay/nhân đôi; contact cooldown; trigger/overlap; không trúng transparent sprite padding; projectile direction và spawn geometry | **Pass** |
| Enemy pressure và spawning | run-pressure config, threat budget và spawner | Các test `DefaultRunPressure_*`, `RunPressureNode_ClampsMinimumVisibleToActiveCap`, `ThreatBudget_BlocksSpecialEnemyButAlwaysAllowsBasicDensity`; `PressureThreatAndGateCadence_RespectRuntimeConstraints` | Nội suy pressure, active/visible cap, budget enemy đặc biệt, density cơ bản và runtime constraint | **Pass**; chưa phải đo FPS |
| Gate system | gate pool, scaling profile, preview, runtime effect | `GateScalingProfileTests` (9); các test gate trong `BalanceV1MathTests`; `BalanceRuntimePlayModeTests` | Phase timing/weights, major cadence và pity, cap preview/runtime, timed modifier pause/expiry, living/dead follower, projectile gate reset | **Pass** |
| Meta upgrade | `PlayerMetaUpgradeService`, `PlayerMetaBalanceConfig`, `SaveService.TryPurchaseUpgrade` | `PlayerMetaUpgradeTrackTests` (10); meta tests trong EditMode/PlayMode | Damage/Fire Rate/Max HP level 0–5; Projectile Count cap 2/value 3; Squad Size cap 3/value 4; migration, idempotency và follower sync | **Pass** cho các assertion hiện có; thiếu test purchase không đủ/exact Coin và unsupported type |
| Economy/Coin | economy config, wallet và reward commit | `Economy_*`, `WalletCoins_*`, Run45 economy tests, mission reward tests | Reward points → Coin, floor time score, wallet chỉ commit khi end run, cost theo track, reward claim một lần | **Pass** |
| Mission progression | `MissionCatalog`, `MissionSystem`, progress evaluator | `MissionSystemPhaseCTests` (7); `MissionRuntimePhaseETests` (15) | BOOT chain, category chain, run/gate/upgrade/final-choice objective, 44/45 và 49/50, 999/1000 single-run kill, 9999/10000 lifetime kill | **Pass** |
| Mission reward và persistence model | `SaveData` mission lists, `SaveService.GrantMissionRewardOnce` | `MissionSavePhaseDTests` (6); reward tests trong `MissionRuntimePhaseETests` | Normalize/deduplicate mission IDs, clone không share list, reset, reward idempotency và Coin consistency | **Pass** trong memory/write path; chưa có reload/corrupt-file test đầy đủ |
| Final Choice và story unlock | `StoryCutsceneUnlockRules`, story runtime, `GameManager`, save | 5 story-rule tests trong `BalanceV1MathTests`; QA rerun; physical-device Final Choice smoke | prerequisite, already-seen, loop 49/50/51/100, survival 419,999/420, alias/branches, save > 50 | **Pass** tự động và **Pass** smoke trên thiết bị được ghi nhận |
| Final Choice → Terminal Protocol | story runtime + `MissionSystem.NotifyFinalChoiceResolved` + Terminal phase rules | `MissionRuntimePhaseETests.FinalChoiceResolved_CompletesFinalMission`; QA rerun; device smoke | Hoàn tất `break_final_choice`; mở hai category head Terminal; mission kế tiếp vẫn khóa theo prerequisite nội bộ | **Pass** |
| Story/dialogue/voice/audio | story catalog/director/playback, gameplay dialogue, voice manifest, audio catalog/mixer | 75 test trong nhóm fixture story–dialogue–voice–audio, gồm 9 PlayMode voice tests | Catalog/ID/CSV deterministic, phase/tag resolution, shuffle/scheduling, playback stop/skip/missing clip, mixer/import/wiring | **Pass** |
| Tutorial/onboarding | tutorial decisions, save flags, transient cutscene và overlay | 27 test trong 5 fixture tutorial/onboarding | Điều kiện mở tutorial, flag persistence model, first-run grant một lần, transient cutscene không đánh dấu story seen, overlay sibling/raycast | **Pass** |
| UI và safe area | `SafeAreaFitter`, hierarchy UI trong `Main.unity` | `SafeAreaFitterTests` (7), `SafeAreaHierarchyPlayModeTests` (1), `08_UI_SafeArea_Audit_After.md` | Insets từng cạnh, zero resolution, orientation/reapply, không nested fitter, các surface quan trọng nằm dưới safe root | **Pass** tự động; manual evidence giới hạn ở scope đã ghi nhận |
| Balance | configs, mathematical model, runtime integration, simulator outputs | `BalanceV1MathTests` (56), `GateScalingProfileTests` (9), `BalanceRuntimePlayModeTests` (10), báo cáo balance | Monotonicity, caps, DPS/durability target model, phase/gate behavior, runtime idempotency | **Pass** về model/assertion; simulator không thay thế human playtest/performance profiling |
| Android build/package | Player settings + APK | `09_Build_Verification.md`; APK hash kiểm tra lại | IL2CPP, ARM64, API 25/36, portrait, ETC2, signature v2, package/version và artifact integrity | **Đạt** cho Beta 0.1.1 artifact được ghi nhận |
| Android device/migration | runtime trên OPPO CPH2059 | `09_Build_Verification.md` | install-over, giữ hash `save.json`/`save.bak`, launch, no app-attributed crash/ANR trong captured logcat, Loop 100 Final Choice smoke, restore save | **Đạt** trên một thiết bị/scenario; chưa đại diện device matrix |

## C. Basis Path verification

### C.1. UT-01 — Purchase Upgrade

**Phạm vi method:** `SaveService.TryPurchaseUpgrade(PlayerMetaUpgradeType type, int cost)`, không cộng complexity của `EnsureLoaded()`, `PlayerMetaUpgradeService` hay các hàm save được gọi bên trong.

**Input:** loại upgrade, cost truyền vào, level và wallet hiện có trong `SaveData`. **Output trực tiếp:** `bool`. **Side effects khi thành công:** trừ `walletCoins`, tăng `lifetimeCoinsSpent`, tăng level đúng một, commit local save/queue cloud upload, phát event `UpgradePurchased`. Khi thất bại, method trả `false` trước khi thay đổi state.

Method có ba quyết định nghiệp vụ theo thứ tự: loại upgrade có được hỗ trợ; level hiện tại còn thấp hơn max; wallet có đủ `safeCost` hay không. Biểu thức đầu dùng short-circuit OR, vì vậy khi dựng control-flow graph cần tách thành hai predicate. Theo cách đếm basis-path trên predicate thực thi, số decision point là 3 và cyclomatic complexity là:

`V(G) = số predicate + 1 = 3 + 1 = 4`.

`Mathf.Max(0, cost)` là lời gọi chuẩn hóa, không phải một nhánh control-flow trong phạm vi method này. Việc `UpgradePurchased?.Invoke(...)` cũng được xem là notification side effect, không phải quyết định nghiệp vụ của flow graph. Nếu tài liệu cũ chỉ đếm số câu lệnh `if`, nó sẽ cho `V(G)=3`; cách đó làm mất đường short-circuit “unsupported type” và không phù hợp với bảng independent path bên dưới.

| Path | Điều kiện đại diện | Kết quả mong đợi | Evidence hiện có |
| --- | --- | --- | --- |
| P1 | `type` không được hỗ trợ | `false`, không đổi Coin/level | `MaxLevels_ArePerUpgradeType` xác nhận MoveSpeed max 0 nhưng **chưa có assertion trực tiếp gọi purchase với unsupported type** |
| P2 | type hợp lệ nhưng `level >= max` | `false`, không đổi Coin/level | `ProjectilePurchase_StopsAtLevelTwo` mua tới level 2 rồi xác nhận lần mua tiếp theo trả `false` |
| P3 | type hợp lệ, chưa max, `walletCoins < safeCost` | `false`, không đổi Coin/level | **Chưa có test trực tiếp cho insufficient Coin hoặc exact boundary `wallet = cost - 1`** |
| P4 | type hợp lệ, chưa max, `walletCoins >= safeCost` | `true`; trừ đúng cost; level +1; commit và phát event | `UpgradePurchase_CompletesDeltaUpgradeMission` kiểm tra success cost 0; `ProjectilePurchase_StopsAtLevelTwo` kiểm tra hai lần mua thành công bằng wallet đã nạp |

Các boundary `wallet == cost`, `wallet == cost - 1`, cost âm được normalize về 0, và việc event không phát ở failure path chưa có regression test chuyên biệt. Do đó UT-01 đại diện tốt cho unit lựa chọn/mua nâng cấp, nhưng flow graph không được chú thích rằng mọi path đã có automated test.

### C.2. UT-02 — Final Choice Eligibility

**Phạm vi method phù hợp:** overload private `StoryCutsceneUnlockRules.IsEligible(Rule rule, SaveData saveData, StoryCutsceneProgressContext context)`. Đây là method thực sự thực thi prerequisite, already-seen và các threshold. Không tính toàn bộ class, vòng lặp lookup của overload public, hoặc logic playback/runtime vào complexity này.

Trong control flow short-circuit có tám predicate:

1. cutscene đã seen;
2. prerequisite ID có tồn tại;
3. prerequisite đã seen;
4. loop đạt min;
5. loop không vượt max;
6. survival đạt min;
7. run kills đạt min;
8. total kills đạt min.

Vì vậy `V(G) = 8 + 1 = 9`. Tập independent path đại diện gồm: already-seen; có prerequisite nhưng chưa seen; dưới min loop; trên max loop; dưới survival; dưới run-kill; dưới total-kill; pass khi không có prerequisite; pass khi có prerequisite và prerequisite đã seen. Nếu chỉ phân tích overload public `IsEligible(string, SaveData, Context)`, complexity riêng của overload lookup là 5 theo cùng quy ước; không được cộng hai method hoặc cả class thành một con số UT-02.

Với riêng Final Choice, rule hiện tại là:

- prerequisite: `CS_06_SystemFatigue` phải đã seen;
- already-seen: `CS_07_FinalChoice_PreChoice` chưa được seen;
- min loop: 50;
- max loop: `int.MaxValue`, tức không có giới hạn trên có ý nghĩa gameplay;
- survival: tối thiểu 420 giây;
- run kills và total kills: đều có min bằng 0, nên không tạo thêm điều kiện hạn chế cho ending;
- ID `CS_07_FinalChoice` được normalize về playable ID `CS_07_FinalChoice_PreChoice`.

Automated evidence trong `StoryCutsceneUnlockRules_UseUpdatedLoopThresholds` kiểm tra thiếu System Fatigue; loop 49/50/51/100; survival 419,999/420; và không replay sau khi PreChoice đã seen. `StoryCutsceneUnlockRules_FinalChoiceMapsAliasAndExposesBranches` kiểm tra alias cùng hai branch Continue Protocol/Shut Down Core. Những test này chạy trong full EditMode job hiện tại và pass.

**Trace runtime của run thứ 50:** `GameManager.HandleSquadDefeated()` → `RunStatsTracker.EndRun()` → `SaveService.RecordRunResult()` → tăng `totalRunsCompleted` → tạo `RunStatsSnapshot` → mission end-run → `StoryCutsceneRuntimeController.TryPlayPostRunCutscene()` → tạo context bằng `saveData.totalRunsCompleted` đã cập nhật → `StoryCutsceneUnlockRules.TryGetFirstEligible()`. Do đó nếu trước khi chết save là 49 run và run hiện tại đạt 420 giây cùng prerequisite, context story nhận loop 50 và có thể mở ending. Đây là kết luận trực tiếp từ thứ tự gọi, không phải suy đoán từ tên biến.

## D. NFR verification matrix

| NFR | Tiêu chí | Evidence | Kết quả | Ghi chú |
| --- | --- | --- | --- | --- |
| Performance | 60 FPS preferred, 30 FPS minimum; frame time ổn định trên Android | Không có Profiler capture, FPS series hay frame-time capture trong repo | **Chưa đủ bằng chứng** | Target không phải measured result; không được kết luận 30/60 FPS |
| Performance / memory | Giảm áp lực asset trên mobile | Texture audit: 891.336.804 → 327.483.116 byte estimated runtime memory (giảm 563.853.688 byte, 63,3%); audio `DecompressOnLoad` 221 → 11, preload 229 → 21; 9 atlas/75 unique sources | **Đạt một phần** | Đây là số liệu import/ước tính asset, không phải peak RAM, GC Alloc/frame hay Memory Profiler measurement |
| Reliability / Data Integrity | Save không mất progression khi update và dữ liệu được normalize/migrate | Schema migration tests; upgrade/mission ID cleanup; reward idempotency; device install-over giữ nguyên SHA-256 của `save.json` và `save.bak`; Loop 100 load thành công | **Đạt một phần** | Chưa có automated corrupt-main → backup fallback, interrupted-write test, reload round-trip đầy đủ hay long-run save soak |
| Usability | UI portrait/safe-area và thao tác chính dùng được | 7 EditMode safe-area test, 1 PlayMode hierarchy test; QA manual cho Main/Pause/Settings/Mission/Game Over/Tutorial/Cutscene; OPPO 1080×2400 | **Đạt một phần** | Chỉ một resolution/device vật lý; không có usability study, accessibility audit hoặc device notch matrix |
| Maintainability | Thay đổi có regression safety và logic tách theo subsystem/config | 31 test fixture, 254/254 pass; ScriptableObject configs; service/rule methods có test; 0 compile error | **Đạt một phần** | Không có code coverage report nên không thể định lượng độ bao phủ; không thấy CI test artifact trong repo |
| Compatibility — package | APK chạy theo cấu hình Android đã công bố | `aapt`/package inspection: API min 25, target 36, ARM64; IL2CPP; portrait; ETC2; APK Signature Scheme v2 | **Đạt** | Kết luận chỉ cho artifact Beta 0.1.1-vc2 đã được kiểm tra |
| Compatibility — thiết bị | Install/launch/update trên thiết bị thật | OPPO CPH2059, Android 11/API 30, ARM64, 1080×2400; `adb install -r`; launch và smoke | **Đạt một phần** | Một thiết bị không chứng minh toàn bộ API 25–36, GPU/vendor và kích thước màn hình |
| Build quality | Candidate build tái truy nguyên được, không có compile/test blocker | APK 69.737.769 byte, SHA-256 khớp; 254/254; 0 compile error; package inspection thành công | **Đạt một phần** | APK ký bằng debug certificate; cần private release keystore trước Play Store/public release; build chưa được tái tạo tại commit audit hiện tại |

## E. Report-ready Vietnamese text

## 3.3 KIỂM THỬ VÀ PHÂN TÍCH KẾT QUẢ

### 3.3.1. Kế hoạch và chiến lược kiểm thử

Hoạt động kiểm thử của True Gate!? được tổ chức theo hướng kết hợp kiểm thử tự động ở mức logic với kiểm thử tích hợp runtime và xác minh build trên thiết bị Android. Mục tiêu không chỉ là xác nhận từng hàm trả về đúng kết quả mà còn kiểm tra sự nhất quán giữa các subsystem có trạng thái dài hạn, gồm vòng đời một lượt chơi, chiến đấu và đội hình, cổng nâng cấp, meta progression, nhiệm vụ, cốt truyện, lưu dữ liệu và giao diện mobile. Môi trường kiểm thử hiện tại sử dụng Unity 6000.4.2f1 và Unity Test Framework 1.6.0. Scene được đưa vào build là `Assets/_Project/Scenes/Main.unity`.

EditMode được sử dụng chủ yếu cho các quy tắc có thể kiểm tra xác định mà không cần mô phỏng nhiều frame, chẳng hạn công thức balance, điều kiện mở cốt truyện, migration save, cap nâng cấp, chuỗi nhiệm vụ, dữ liệu voice và phép tính safe area. PlayMode được sử dụng cho những hành vi phụ thuộc vòng đời GameObject, coroutine, physics/collider, object instantiation và cấu trúc scene, chẳng hạn projectile spawn, enemy contact cooldown, hitbox, đồng bộ follower, phát voice, tutorial overlay và safe-area hierarchy. Ngoài hai nhóm tự động này, build verification tách thành hai lớp khác: kiểm tra package APK bằng công cụ Android và smoke test trên thiết bị vật lý. Việc tách lớp giúp tránh đồng nhất “test pass trong Editor” với “APK đã chạy đúng trên điện thoại”.

Phạm vi audit gồm 31 tệp test: 24 fixture EditMode và 7 fixture PlayMode. Unity Test Runner phát hiện 213 test EditMode và 41 test PlayMode. Toàn bộ suite đã được chạy lại trên working tree ngày 08/08/2026, không chỉ kế thừa báo cáo QA cũ. Kết quả đạt 254/254, không có test thất bại hoặc bị bỏ qua, và Unity Compilation Pipeline không ghi nhận lỗi biên dịch C#.

### 3.3.2. Kiểm thử đơn vị theo Basis Path

#### UT-01 — Purchase Upgrade

UT-01 phân tích method `SaveService.TryPurchaseUpgrade(PlayerMetaUpgradeType type, int cost)`. Input của unit gồm loại nâng cấp, chi phí truyền vào và trạng thái đang lưu của người chơi; output trực tiếp là giá trị Boolean. Khi thành công, unit trừ Coin, tăng tổng Coin đã chi, tăng level đúng một bậc, lưu trạng thái và phát sự kiện purchase. Khi thất bại, unit thoát trước các phép thay đổi này.

Ba decision point nghiệp vụ là: upgrade có được hỗ trợ hay không, level có còn thấp hơn mức tối đa hay không, và ví có đủ chi phí đã chuẩn hóa hay không. Do điều kiện đầu sử dụng short-circuit, flow graph được tách thành ba predicate và cyclomatic complexity là `V(G)=4`. Bốn đường độc lập tương ứng với: loại upgrade không hợp lệ; upgrade đã đạt max; Coin không đủ; và giao dịch thành công.

Kết quả đối chiếu test cho thấy đường max-level và đường thành công đã có automated evidence. `ProjectilePurchase_StopsAtLevelTwo` mua Projectile Count từ level 0 lên 2, xác nhận giá trị tối đa là ba projectile, sau đó xác nhận lần mua tiếp theo thất bại và level không tăng. `UpgradePurchase_CompletesDeltaUpgradeMission` xác nhận một purchase thành công đồng thời cập nhật objective nhiệm vụ. Tuy nhiên, suite chưa có test trực tiếp cho unsupported type, wallet đúng bằng cost và wallet nhỏ hơn cost một đơn vị. Vì vậy báo cáo chỉ kết luận logic hiện tại đã pass ở các đường có assertion, không khẳng định cả bốn đường đã được khóa bằng regression test.

#### UT-02 — Final Choice Eligibility

UT-02 chọn overload private `StoryCutsceneUnlockRules.IsEligible(Rule, SaveData, StoryCutsceneProgressContext)`, vì đây là phạm vi thực thi đầy đủ các điều kiện eligibility. Tám predicate gồm trạng thái already-seen, sự tồn tại và trạng thái của prerequisite, min/max loop, survival, run kills và total kills. Cyclomatic complexity của method là `V(G)=9`. Không cộng complexity của vòng lặp tra rule trong overload public hoặc các method khác của class.

Đối với Final Choice, implementation yêu cầu `CS_06_SystemFatigue` đã được xem, `CS_07_FinalChoice_PreChoice` chưa được xem, số run tối thiểu là 50 và thời gian sống của run hiện tại tối thiểu là 420 giây. Max loop được đặt bằng `int.MaxValue`, vì vậy không có upper bound có ý nghĩa trong gameplay. Run kills và total kills của rule này đều có ngưỡng 0. Alias `CS_07_FinalChoice` được chuẩn hóa về playable cutscene `CS_07_FinalChoice_PreChoice`.

Các test thực hiện boundary-value analysis tại run 49, 50, 51 và 100; thời gian 419,999 và 420 giây; prerequisite chưa xem; và Final Choice đã xem. Kết quả xác nhận run 49 chưa đủ, run 50 vừa đạt ngưỡng, các save run 51 và 100 vẫn hợp lệ, 419,999 giây chưa đạt, còn đúng 420 giây thì đạt. Sau khi PreChoice đã được ghi nhận, alias Final Choice cũng không thể mở lại.

Thứ tự runtime giải thích vì sao run thứ 50 có thể mở ending. Khi đội hình bị hạ, `GameManager` gọi `RunStatsTracker.EndRun()`. Method này gọi `SaveService.RecordRunResult()`, làm `totalRunsCompleted` tăng từ 49 lên 50 trước khi snapshot được chuyển cho story runtime. `StoryCutsceneRuntimeController` sau đó đọc lại save đã cập nhật để tạo eligibility context. Vì vậy điều kiện loop được đánh giá với giá trị 50, không phải 49.

### 3.3.3. Kết quả kiểm thử tự động

| Loại kiểm thử | Passed | Failed | Skipped | Tổng |
| --- | ---: | ---: | ---: | ---: |
| EditMode | 213 | 0 | 0 | 213 |
| PlayMode | 41 | 0 | 0 | 41 |
| **Tổng** | **254** | **0** | **0** | **254** |

Các test không tập trung vào một module duy nhất. Nhóm core gameplay và balance kiểm tra công thức damage/fire rate, squad, enemy pressure, gate phase, cap và economy. Nhóm meta progression kiểm tra năm track Damage, Fire Rate, Max HP, Projectile Count và Squad Size, gồm level table, cost, cap, migration và việc áp dụng stat sang main unit/follower. Nhóm mission kiểm tra BOOT chain, category prerequisite, objective theo run/gate/upgrade/kill/final choice, reward và save model. Nhóm story–dialogue–voice kiểm tra điều kiện mở cutscene, mapping voice, parser CSV, scheduler, playback và trạng thái seen. Nhóm runtime kiểm tra collider, hitbox, projectile, enemy contact, UI raycast, tutorial overlay và safe-area hierarchy.

Các boundary đáng chú ý gồm 44/45 và 49/50 run đối với nhiệm vụ, 999/1000 kill trong một run, 9999/10000 lifetime kill, loop 49/50/51/100 và survival 419,999/420 đối với Final Choice, projectile meta level 0/1/2 với giá trị 1/2/3, squad meta level 0–3 với giá trị 1–4, cùng migration level cũ 5 về cap mới. Những mốc này có assertion trực tiếp nên có giá trị regression cao hơn việc chỉ chạy một happy path.

### 3.3.4. Phân tích kết quả kiểm thử

**Tính đúng chức năng.** Kết quả 254/254 chứng minh rằng tại commit và môi trường đã chạy, toàn bộ assertion hiện được đăng ký trong Unity Test Runner đều thỏa mãn. Bằng chứng mạnh nhất nằm ở các quy tắc xác định: công thức balance, cap meta, chain nhiệm vụ, Final Choice boundary, reward idempotency, save normalization, hitbox/contact và playback state. Kết quả này cũng chứng minh các fixture PlayMode có thể khởi tạo và hoàn thành mà không tạo test failure. Tuy nhiên, 254 test pass không đồng nghĩa 100% code coverage, không chứng minh mọi scene/device path, không chứng minh cloud save, và không chứng minh hiệu năng 30/60 FPS. Repository không có code coverage report nên không được quy đổi số test thành phần trăm bao phủ.

**Độ ổn định regression.** QA trước rerun từng ghi nhận hai lỗi EditMode và sáu lỗi PlayMode, gồm Destroy trong EditMode, expected DPS cũ, fixture thiếu FirePoint và assertion projectile vượt cap runtime. Rerun ngày 31/07 xác nhận các lỗi này đã được xử lý mà không disable test; rerun hiện tại tiếp tục đạt 213/213 và 41/41. Một regression quan trọng khác là Terminal mission từng kế thừa sai prerequisite category từ phase trước. Test `FinalChoiceResolved_CompletesFinalMission` hiện khóa chuỗi `Final Choice → break_final_choice → TERMINAL PROTOCOL`, đồng thời xác nhận các mission Terminal cấp sau chưa được mở sớm. Các test contact/hitbox cũng khóa rủi ro collider bị thay đổi theo sprite padding và repeated contact bỏ qua cooldown.

**Bao phủ giá trị biên.** Final Choice có tập biên rõ nhất: dưới/đúng/trên loop threshold và dưới/đúng survival threshold. Meta progression kiểm tra cap khác nhau giữa ba track số thực và hai track số lượng; đặc biệt Projectile Count dừng ở level 2 và Squad Size dừng ở level 3. Mission test kiểm tra ngay trước và đúng ngưỡng run/kill. Schema migration kiểm tra dữ liệu cũ có level số lượng vượt cap nhưng vẫn giữ wallet, story và tutorial. Điểm còn thiếu là UT-01 chưa có assertion trực tiếp cho Coin thiếu một đơn vị, Coin đúng bằng cost và loại upgrade không hỗ trợ.

**Độ tin cậy lưu trữ.** Save model chuẩn hóa giá trị âm, bảo đảm list không null, loại ID rỗng/trùng, clamp level nâng cấp và tăng schema lên version hiện tại. Mission reward dùng danh sách ID đã cấp để ngăn cộng Coin hai lần. Local repository dùng quy trình temp–backup–main và fallback sang backup khi đọc main thất bại. Bên cạnh automated tests, install-over trên OPPO giữ nguyên SHA-256 của cả `save.json` và `save.bak`, và save Loop 100 mở Final Choice thành công trước khi save gốc được khôi phục byte-for-byte. Dù vậy, chưa có automated test chủ động corrupt main file, mô phỏng gián đoạn giữa các bước ghi hoặc tải lại toàn bộ trạng thái bằng service instance mới; vì vậy reliability được đánh giá đạt một phần thay vì tuyệt đối.

**Giá trị tích hợp runtime.** 213 EditMode test cung cấp phản hồi nhanh và cô lập tốt cho logic/config/data. 41 PlayMode test bổ sung loại bằng chứng khác: MonoBehaviour lifecycle, coroutine/frame progression, physics/collider geometry, instantiate/destroy, scene hierarchy, AudioSource state và UI sibling/raycast. Ví dụ, công thức Projectile Count đúng trong EditMode chưa đủ chứng minh volley được spawn đúng vị trí; PlayMode kiểm tra single/multiple projectile quanh FirePoint. Tương tự, safe-area math đúng chưa đủ chứng minh scene không lồng nhiều fitter; PlayMode kiểm tra hierarchy của `Main.unity`. Vì vậy hai suite bổ trợ nhau, không thể thay thế cho nhau.

**Giới hạn.** Automated suite chưa có performance profiling, code coverage, corrupt-save recovery, full reload persistence, cloud-save conflict integration, release signing hoặc device matrix. Final Choice có rule-level automation và physical smoke, nhưng chưa có một PlayMode end-to-end test tự chạy toàn chuỗi death → increment run 50 → hiện cutscene → chọn branch → mở Terminal mission. Các hạn chế này không phủ nhận kết quả pass, nhưng giới hạn phạm vi kết luận có thể rút ra.

### 3.3.5. Đánh giá yêu cầu phi chức năng

| NFR | Tiêu chí | Evidence | Kết quả | Ghi chú |
| --- | --- | --- | --- | --- |
| Performance | Mục tiêu 60 FPS, tối thiểu 30 FPS | Không có Profiler/FPS/frame-time artifact | **Chưa đủ bằng chứng** | Cần đo trên nhiều thiết bị và scenario áp lực cao |
| Reliability/Data Integrity | Giữ progression, migrate/normalize và ngăn reward trùng | Automated migration/idempotency; install-over giữ nguyên hash save; Loop 100 smoke | **Đạt một phần** | Thiếu corrupt-file/reload/soak automation |
| Usability | Portrait UI không bị che, các luồng chính thao tác được | Safe-area EditMode/PlayMode; manual smoke ở 1080×2400 | **Đạt một phần** | Thiếu khảo sát người dùng, accessibility và device matrix |
| Maintainability | Logic có regression suite và tách theo cấu hình/subsystem | 31 fixture, 254/254, 0 compile error | **Đạt một phần** | Không có code coverage và CI artifact |
| Compatibility | APK IL2CPP ARM64, API min 25/target 36, chạy trên Android thật | Package inspection và OPPO Android 11 smoke | **Đạt một phần** | Package đạt; device coverage mới có một thiết bị |
| Build quality | APK truy nguyên được và không có blocker tự động | Size/hash/signature v2; test/compile pass | **Đạt một phần** | Debug certificate; chưa đủ điều kiện public distribution |

### 3.3.6. Tối ưu hóa cho thiết bị di động

Các audit asset ghi nhận thay đổi có thể định lượng. Trên 338 texture record, tổng `EstimatedRuntimeMemory` giảm từ 891.336.804 xuống 327.483.116 byte, tương đương giảm 563.853.688 byte hoặc 63,3%. Số audio clip dùng `DecompressOnLoad` giảm từ 221 xuống 11; số clip preload giảm từ 229 xuống 21. Chín sprite atlas ETC2 được tạo cho 75 texture nguồn duy nhất; audit không phát hiện source trùng giữa các atlas và loại các background lớn khỏi atlas. Safe-area được áp dụng cho Main menu, HUD, Pause, Settings, Mission Log, Game Over, Tutorial và runtime cutscene theo cấu trúc đã kiểm tra.

Những số liệu trên chứng minh công việc tối ưu asset/import và cấu trúc mobile đã được thực hiện. Chúng không phải số đo peak RAM hoặc FPS. Repository chưa lưu Profiler capture, GC Alloc/frame, CPU/GPU frame time hay long-run benchmark trên Android. Vì vậy kết luận phù hợp là: **các tối ưu asset/build đã được xác minh, nhưng chưa có đủ dữ liệu runtime profiling để kết luận chắc chắn NFR FPS/frame time/memory trên nhiều thiết bị.**

### 3.3.7. Kiểm thử build và thiết bị Android

Build verification của Beta 0.1.1 xác nhận Unity 6000.4.2f1, scene `Main.unity`, application ID `com.mimicompany.truegate`, version `0.1.1` code 2, IL2CPP, ARM64-only, minimum API 25, target API 36, portrait-only, ETC2 và non-development build. APK được kiểm tra có kích thước 69.737.769 byte, SHA-256 `0454614E4C4856D126AC6688F2A4034ABD539E579DFC160B38AA4C238CBB7684`; hash này đã được audit kiểm tra lại trên artifact hiện có. `aapt` và package inspection xác nhận version/API/ABI; `apksigner verify` xác nhận APK Signature Scheme v2.

Ba loại bằng chứng cần được phân biệt. Thứ nhất, **automated test** gồm 213 EditMode, 41 PlayMode và compilation check. Thứ hai, **build/package inspection** kiểm tra metadata, ABI, API, kích thước, hash và chữ ký APK. Thứ ba, **physical-device verification** được thực hiện trên OPPO CPH2059, Android 11/API 30, ARM64, độ phân giải 1080×2400. Thiết bị được update bằng `adb install -r` từ Beta 0.1.0 lên 0.1.1; first install time và hash hai save file được giữ nguyên. Logcat được ghi nhận không có fatal Java exception, native fatal signal hoặc ANR do package True Gate trong scenario đã chạy.

Final Choice smoke trên thiết bị dùng fixture Loop 100 tạm thời. Save tải thành công, run kéo dài 602,616394 giây làm `totalRunsCompleted` tăng 100→101, Final Choice xuất hiện, branch `CONTINUE PROTOCOL` hoàn tất, `finalChoiceResolved` được lưu, `break_final_choice` hoàn thành và hai entry Terminal đầu tiên được tạo. Sau đó save gốc được restore và xác nhận lại bằng SHA-256. Bằng chứng này bổ sung cho rule-level automation vì nó đi qua APK và persistence thật, nhưng vẫn chỉ là một device/scenario.

Candidate hiện dùng debug signing certificate, phù hợp sideload beta cục bộ nhưng chưa phù hợp Play Store hoặc phát hành công khai. Ngoài ra, build report gắn với release source commit `29bc438...`; audit hiện tại chỉ chạy lại test chứ không rebuild APK ở commit `d6954fe`. Do đó kết luận build phải gắn với artifact Beta 0.1.1-vc2 đã xác minh, không mở rộng mặc định sang mọi build mới hơn.

## F. Missing evidence / limitations

1. Không có actual code coverage report; không được claim 254 test tương đương 100% coverage.
2. Không có Unity Profiler capture, FPS/frame-time measurement, GC Alloc/frame, peak RAM, Memory Profiler snapshot, CPU/GPU trace, Android profiler log hoặc long-run performance soak. Balance telemetry không phải performance telemetry.
3. Không có automated test cho `save.json` corrupt rồi fallback `save.bak`, file write bị gián đoạn tại `save.tmp`, hoặc recovery sau crash giữa copy/delete/move.
4. Chưa có automated round-trip test tạo service/repository mới từ cùng thư mục để xác nhận mission state, wallet, upgrade và cutscene state tải lại đồng thời.
5. UT-01 thiếu test trực tiếp cho unsupported upgrade, insufficient Coin, exact Coin, cost âm được clamp về 0 và event không phát khi thất bại.
6. Chưa có PlayMode end-to-end test cho chuỗi death ở run 50 → save increment → Final Choice UI → branch completion → Terminal Protocol. Hiện evidence được ghép từ unit/integration test, QA trace và physical-device smoke.
7. Cloud provider mặc định là `NoOpCloudSaveProvider`; suite không chứng minh cloud merge/conflict/upload với provider thật.
8. Device verification mới có OPPO CPH2059 Android 11/API 30, chưa có matrix cho API 25–36, nhiều GPU/vendor, notch/aspect ratio và thiết bị RAM thấp.
9. APK Beta 0.1.1-vc2 ký bằng debug certificate; chưa có release-keystore evidence. APK `TrueGate_0.1.1_NewIcon.apk` mới hơn theo timestamp chưa có report truy nguyên tương đương.
10. Build Android chưa được tái tạo tại commit audit `d6954fe`; build conclusion vẫn gắn với source commit và artifact ghi trong `09_Build_Verification.md`.
11. Manual safe-area capture được tài liệu dẫn tới `Temp/MobileReadiness/`, nhưng thư mục artifact này không còn hiện diện trong workspace; kết luận manual dựa trên report đã lưu, không dựa trên việc audit xem lại capture gốc.
12. Chưa có usability study, accessibility verification, store-install test hoặc production monitoring/ANR telemetry dài hạn.

### Đề xuất bằng chứng bổ sung theo mức ưu tiên

1. Thêm 4–6 EditMode test nhỏ cho các path còn thiếu của `TryPurchaseUpgrade()` và fallback/corrupt-save; đây là phần có chi phí thấp nhưng tăng traceability rõ nhất.
2. Thêm một PlayMode regression end-to-end cho Final Choice/Terminal Protocol bằng fixture deterministic, không cần mô phỏng 50 run thật.
3. Chạy Unity Profiler hoặc Profile Analyzer trên ít nhất ba nhóm thiết bị/scenario: early run, áp lực enemy gần cap và long run qua mốc 420–600 giây; lưu FPS percentile, CPU/GPU frame time, GC Alloc/frame và peak memory cùng device/build hash.
4. Rebuild bản release-signed từ commit dùng cho báo cáo, lặp lại package inspection, install-over và smoke test trên một device matrix tối thiểu; lưu log/capture dưới một thư mục evidence được version-control hoặc artifact store quản lý.
