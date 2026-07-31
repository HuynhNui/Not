# Game Design Document - True Gate

## 1. Kiểm soát tài liệu

| Mục | Giá trị |
|---|---|
| Project | True Gate |
| Thể loại | Portrait survival auto-shooter + gate choice + meta progression |
| Nền tảng hiện tại | Android mobile |
| Build | Beta 0.1.0 (version code 1) |
| GDD version | 1.0-current |
| Cập nhật | 2026-07-31 |
| Owner | Huỳnh Núi |
| Trạng thái | Direct-install Android beta |

Tài liệu này mô tả **game đang tồn tại trong project**, không phải concept tương lai. Khi thông số trong GDD và ScriptableObject khác nhau, asset balance được gán trong `Main.unity` là nguồn dữ liệu chính.

Nguồn tham chiếu kỹ thuật:

- Scene build: `Assets/_Project/Scenes/Main.unity`.
- Balance active: `BalanceBootstrapConfig_v1_4_1_MetaProgression.asset`.
- Mission: `MissionCatalog.cs` và `MissionCatalog_v1.asset`.
- Story unlock: `StoryCutsceneUnlockRules.cs`.
- Save schema: `SaveData.cs`.
- Kết quả mobile: `Assets/_Project/Documentation/MobileReadiness/`.

## 2. Tổng quan sản phẩm

### 2.1 High Concept

True Gate là game mobile 2D pixel-art, trong đó người chơi điều khiển UNIT-07 di chuyển ngang để sống sót trước các đợt kẻ địch. Đội hình tự động bắn lên phía trước. Cứ mỗi 15 giây, người chơi chọn một trong ba gate để thay đổi sức mạnh, khả năng sinh tồn hoặc mức áp lực của run.

Sau mỗi run, coin được đưa vào ví vĩnh viễn để mua năm nhóm nâng cấp. Mission và story cutscene biến các lần chết, hồi sinh và mạnh lên thành một phần của câu chuyện về một AI dần nhận ra mình đang phục vụ một cuộc xâm lược.

### 2.2 Design Pillars

1. **Survival pressure:** mật độ và sức mạnh kẻ địch tăng theo thời gian.
2. **Quyết định nhanh:** ba gate xuất hiện ngay trong run, gồm buff an toàn, utility, trade-off và major reward.
3. **Power growth rõ ràng:** damage, fire rate, HP, projectile và squad đều có thay đổi nhìn thấy được.
4. **Loop có ý nghĩa:** mỗi run tích lũy coin, mission, kỷ lục và story progress.
5. **Mobile readability:** portrait, touch một ngón, Safe Area, UI pixel rõ ràng trên màn hình hẹp.

## 3. Narrative

Người chơi là **UNIT-07**, một đơn vị AI do con người đưa đến hành tinh xa lạ để diệt sinh vật bản địa, tái định cư và khai thác tài nguyên. Những sinh vật bị gọi là quái vật thực chất đang bảo vệ nơi sống của chúng.

Meta progression là một phần của fiction: khi UNIT-07 bị tiêu diệt, core bị thu hồi, tái tạo và đưa trở lại chiến trường. Các memory fragment tồn tại qua nhiều loop khiến UNIT-07 dần nhận ra bản chất của nhiệm vụ.

Story đi từ trạng thái phục tùng đến thức tỉnh, rồi kết thúc bằng hai lựa chọn:

- **Continue Protocol:** tiếp tục chu kỳ chiến đấu.
- **Shut Down Core:** tự ngắt core để từ chối tiếp tục làm công cụ hủy diệt.

Thông điệp trung tâm là phản chiến, quyền tự quyết và cái giá của việc lặp lại bạo lực.

## 4. Core Gameplay

### 4.1 Run Loop

1. Từ Main Menu, người chơi chọn `START RUN`.
2. Tutorial gameplay chạy nếu save chưa hoàn thành phiên bản tutorial hiện tại.
3. Đội hình tự động bắn; người chơi kéo ngang để né và căn lane.
4. Enemy spawn liên tục và scale theo thời gian.
5. Mỗi 15 giây, ba gate được tạo; chọn một gate khóa hai gate còn lại.
6. Run kết thúc khi toàn bộ squad bị hạ.
7. Game Over hiện time, score, kill, coin và best records.
8. Coin được lưu để mua upgrade; mission và story được đánh giá từ progress vừa ghi.
9. Người chơi retry, mở Upgrade hoặc về Home.

### 4.2 Điều khiển

- Mobile: chạm và kéo để điều khiển trục X. Vị trí Y của đội hình được giữ trong safe gameplay zone.
- Editor/desktop: giữ chuột trái và kéo ngang.
- Chạm trên UI không điều khiển player.
- Bắn là tự động; không có nút fire.
- Pause khóa gameplay controls và dừng run.

### 4.3 Win/Lose

- **Lose condition:** main unit và tất cả follower đều chết.
- **Run mode:** endless survival, không có đích đến cố định.
- **Narrative completion:** giải quyết final choice.
- **Mission completion cuối:** `terminal_250000_total_kills`.

## 5. Player, Squad và Combat

### 5.1 Player Squad

- Squad gồm main UNIT-07 và follower.
- Mỗi thành viên còn sống tự động bắn.
- Follower xếp thành rear-arc formation và đồng bộ damage, fire rate, HP, projectile count từ main unit.
- Khi main unit chết mà follower còn sống, hệ thống có thể promote follower; run chỉ kết thúc khi squad không còn unit sống.

### 5.2 Permanent Upgrades

Balance active: `balance-v1.4.1-meta-stat-progression`.

| Track | Giá trị Lv.0 -> Max | Max level | Chi phí từng lần mua |
|---|---|---:|---|
| DMG | 3.25, 3.5, 4, 4.5, 4.75, 5 | 5 | 4k, 12k, 30k, 65k, 139k |
| FIRE | 4, 4.4, 4.8, 5.2, 5.8, 6.4 | 5 | 4k, 10k, 24k, 52k, 100k |
| HP | 10, 11.5, 13, 15, 17.5, 20 | 5 | 3k, 8k, 20k, 40k, 69k |
| BULLET | 1, 2, 3 | 2 | 15k, 55k |
| PLAYER | 1, 2, 3, 4 | 3 | 12k, 48k, 140k |

`MoveSpeed` vẫn tồn tại trong enum để tương thích dữ liệu cũ, nhưng không phải upgrade track được hỗ trợ trên UI.

### 5.3 Projectile

- Projectile bay theo hướng bắn, gây damage và được quản lý qua pool khi prefab/config hỗ trợ.
- Runtime có modifier cho homing, pierce và split.
- Projectile count từ meta có cap 3; gate major có thể tăng projectile trong run theo run cap.

## 6. Enemy System

Enemy role active và base stat trước khi nhận time scaling:

| Role | Mô tả | Unlock | HP | Speed | Damage | Score | Threat |
|---|---|---:|---:|---:|---:|---:|---:|
| Basic | Melee cơ bản | 0s | 2 | 2.8 | 0.5 | 1 | 0 |
| Chomboom | Melee nổ | 30s | 6 | 2.5 | 3 | 4 | 1.5 |
| Vomfy | Ranged attacker | 90s | 8 | 2.7 | 1.5 | 5 | 2 |
| Swarmer | Nhẹ, nhanh, xuất hiện theo nhóm | 120s | 1 | 5.2 | 0.5 | 1 | 0.25 |
| Elite | Mục tiêu nguy hiểm, thưởng cao | 180s | 36 | 1.85 | 4.5 | 50 | 8 |
| Tanker | Chậm, nhiều HP | 210s | 30 | 1.6 | 3 | 10 | 3 |

Run pressure nội suy giữa các mốc 0, 60, 180, 300, 420 và 720 giây. Active cap tăng từ 12 lên 60, spawn rate từ 3 lên 12 enemy/giây, HP multiplier từ 1 lên 4.5 và damage multiplier từ 0.75 lên 1.9.

Enemy contact damage có cooldown dùng chung cho trigger và overlap path để tránh gây nhiều hit trong cùng một khoảng va chạm.

## 7. Gate System

### 7.1 Offer Rules

- Cadence thường: 15 giây.
- Mốc major eligibility: 60 giây.
- Mỗi set có 3 gate trên 3 lane responsive.
- Hệ thống đảm bảo một tỷ lệ buff tối thiểu và có pity/telemetry cho Major gate.
- Chọn một gate sẽ hủy hoặc khóa các lựa chọn còn lại.
- Tutorial gate không tính vào lifetime mission progress.

### 7.2 Categories và Effects

| Category | Gate hiện tại | Tác dụng chính |
|---|---|---|
| Stable | Damage, Fire Rate, Vitality | Tăng stat không có drawback |
| Utility | Repair, Barrier, Freeze | Hồi HP, chặn hit, làm chậm enemy |
| Risky | Glass Cannon, Bullet Storm, Reinforcement, Bounty | Buff lớn kèm incoming damage/enemy pressure/temporary trade-off |
| Major | Projectile, Recruit, Overclock | Tăng projectile, squad hoặc damage + fire rate |

Magnitude gate scale theo phase của run trong `GateScalingProfile_v1_3_3_EliteSquad.asset`. Gate có thể thay đổi damage, fire rate, max HP, heal, barrier hit, enemy speed, projectile, squad size, incoming damage, enemy pressure và coin multiplier.

## 8. Economy và Records

- Wallet coin là tài nguyên vĩnh viễn dùng để mua meta upgrade.
- Coin run dựa trên tổng reward point của enemy, nhân `rewardScale = 0.85`; không có coin thụ động theo thời gian.
- Bounty gate có thể nhân coin reward trong run.
- Mission thưởng coin sau khi người chơi bấm `CLAIM` trong Mission Log; reward được cấp idempotent một lần.
- Score = kill score + 0.5 điểm mỗi giây sống + elite bonus.
- Save lưu best survival time, best kills, best coins và best score.
- Không có nút debug `+10k coin` trong build.

## 9. Mission System

Catalog hiện tại có **47 mission**:

| Phase | Số mission | Vai trò |
|---|---:|---|
| BOOT | 6 | Tutorial, first run, upgrade và gate onboarding |
| OBSERVE | 7 | Survival/combat/meta mức đầu |
| MEMORY LEAK | 6 | Loop, gate, survival và upgrade trung cấp |
| HUMAN COMMAND | 7 | Combat, loop, survival và squad |
| SYSTEM FATIGUE | 6 | Major gate, combat và max upgrade |
| BREAK THE CYCLE | 6 | Endgame trước final choice |
| TERMINAL PROTOCOL | 9 | Post-choice kill challenge dài hạn |

Quy tắc runtime:

- BOOT mở tuần tự từng mission.
- Sau BOOT, mission chạy theo các category chain song song: survival, run kills, total kills, loop, gate, upgrade, squad và story.
- Progress mode gồm `AbsoluteLifetime`, `DeltaSinceUnlock` và `BestSingleRun`.
- Mission complete được lưu ngay, đánh dấu unread và có reward để claim một lần.
- `break_final_choice` hoàn tất khi một trong hai final branch được ghi nhận.
- Toàn bộ `TERMINAL PROTOCOL` bị khóa trước final choice.
- Sau final choice, terminal run-kill và total-kill chain bắt đầu lại trong chính phase Terminal; các mission sau vẫn cần mission trước cùng category.

Mission cuối của toàn catalog là `terminal_250000_total_kills`.

## 10. Story và Cutscenes

Runtime có 7 mốc story chính, voice theo từng line và nút Skip:

| Cutscene | Điều kiện mở khóa |
|---|---|
| CS_01 Boot Sequence | Save mới, chưa xem |
| CS_02 First Death Recovery | Đã xem CS_01, loop >= 1 |
| CS_03 Enemy Does Not Charge | Đã xem CS_02, loop >= 3, sống >= 30s, run kills >= 100 |
| CS_04 Gate Memory Leak | Đã xem CS_03, loop >= 10, sống >= 180s |
| CS_05 Human Command | Đã xem CS_04, loop >= 20, sống >= 300s, total kills >= 1,000 |
| CS_06 System Fatigue | Đã xem CS_05, loop >= 35, sống >= 360s |
| CS_07 Final Choice | Đã xem CS_06, đã hoàn thành >= 50 run, sống >= 420s và chưa xem CS_07 |

CS_07 phát pre-choice, sau đó chuyển sang `Continue Protocol` hoặc `Shut Down Core`. Run vừa kết thúc đã được cộng vào `totalRunsCompleted` trước khi kiểm tra điều kiện, vì vậy run thứ 50 có thể mở ending; save đã vượt mốc 50 vẫn hợp lệ và không cần reset. Lựa chọn được lưu và thông báo cho MissionSystem để hoàn tất `break_final_choice`.

Gameplay dialogue riêng được lập lịch theo psychology/story phase và có thể tiếp tục không audio nếu một voice clip bị thiếu.

## 11. Tutorial và UI Flow

### 11.1 Tutorial

- Gameplay tutorial: intro, movement, auto-fire, enemy warning, gate và complete.
- Upgrade onboarding: recovery, mở panel, coin, upgrade row, purchase và complete.
- Tutorial và cutscene đều có Skip; tutorial skip ở góc dưới màn hình.

### 11.2 Screens

`Main Menu`, `Gameplay HUD`, `Upgrade`, `Mission`, `Settings`, `Pause`, `Game Over` và cutscene overlay cùng tồn tại trong một build scene.

- HUD: survival time, coin, enemy defeated, score, health và Pause.
- Upgrade: currency, power, squad và năm upgrade row.
- Mission: active/unlocked/completed state, progress, notification và Claim.
- Pause: Resume, Restart, Settings, Home, Music và SFX.
- Settings: Music, SFX, Vibration, Damage Text và Reset Data có confirm popup.
- Game Over: final stats, best records, Retry, Upgrade và Home.

Tất cả panel build chính dùng Safe Area; Mission panel có `MissionSafeAreaRoot` riêng.

## 12. Audio và Visual Direction

- Visual: 2D pixel-art, portrait, nền sci-fi/alien, UI viền xanh trắng với màu category rõ ràng.
- Audio mixer tách Music, Ambience, SFX, UI và Dialogue.
- BGM gồm Main Menu, Gameplay Normal, Gameplay Pressure, Story và Ending.
- Cue chính gồm shot, hit, Chomboom explosion, gate freeze, run start, squad defeated, mission complete, UI và final choice.
- Music/SFX toggle hoạt động ở Settings và Pause, lưu bằng PlayerPrefs sau khi khởi động lại.
- Damage Text toggle ẩn/hiện combat numbers; Vibration là setting riêng.
- Story voice được stream/compress phù hợp mobile và không preload hàng loạt.

## 13. Technical Architecture và Save

### 13.1 Runtime

- Unity 6000.4.2f1, URP 2D, Input System, UGUI/TMP.
- Một scene build: `Main.unity`.
- `GameManager` điều phối StateMachine, combat, spawner, gate, UI, mission, tutorial, story, audio và telemetry.
- Game states: Bootstrap, MainMenu, Playing, Cutscene, Paused, GameOver.
- Data balance dùng ScriptableObject; pool được dùng cho enemy/projectile/effect có hỗ trợ.

### 13.2 Save

- Save schema hiện tại: 11.
- Local save lưu records, wallet, lifetime stats, upgrades, tutorial, mission, seen cutscenes và final choice.
- Mission reward và upgrade purchase được bảo vệ khỏi cấp/trừ tiền lặp.
- Có abstraction cloud save và conflict resolution, nhưng provider runtime hiện tại là no-op; không được mô tả như cloud save đang hoạt động.
- Audio/settings toggle lưu bằng PlayerPrefs, tách khỏi progression save.
- Reset Data cần xác nhận trước khi xóa progress.

## 14. Mobile Build Target

| Setting | Giá trị hiện tại |
|---|---|
| Application ID | `com.mimicompany.truegate` |
| Orientation | Portrait only |
| Scripting backend | IL2CPP |
| Architecture | ARM64 |
| Minimum Android | API 25 |
| Target Android | API 36 theo RC manifest |
| Texture format | ETC2 |
| Release status | Direct-install beta ready |

RC đã được smoke-test trên OPPO CPH2059, Android 11, 1080 x 2400 mà không có crash/ANR trong flow đã kiểm tra. APK beta hiện tại dùng debug certificate; cần private release keystore trước khi phát hành công khai hoặc lên Play Store.

## 15. QA và Current Scope

QA bắt buộc trước mỗi RC:

1. C# compile không có error.
2. EditMode và PlayMode test được chạy và lưu report.
3. Main scene và project asset không có missing reference.
4. Main Menu -> Tutorial/Skip -> Gameplay -> Gate -> Pause -> Settings -> Game Over được smoke-test.
5. Upgrade, mission claim, final choice và save persistence được kiểm tra.
6. Android APK được verify package/version/signature và cài bằng `adb install -r` để giữ save.

Phạm vi chưa có trong beta hiện tại:

- Không có boss fight.
- Không có leaderboard.
- Không có cloud provider production.
- Không có monetization/ads flow đang hoạt động.
- Không có iOS build đã xác minh.
- Chưa có private Play Store signing key.

Kết quả test mới nhất được lưu trong `Assets/_Project/Documentation/QA/` thay vì ghi cùng vào GDD để tài liệu gameplay không bị lỗi thời sau mỗi lần chạy test.
