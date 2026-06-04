## **Game Design Document — True Gate?**



**1. Document Control**

| **Mục** | **Nội dung** |
| --- | --- |
| Project Name | True Gate ? |
| Genre | Survival Auto Shooter + Gate Upgrade |
| Platform | Mobile |
| Target Build | Production-ready |
| Document Version | v0.1 |
| Last Updated | TBD |
| Owner | Huỳnh Núi |
| Status | In Development |

**1.1 Mục tiêu của document**

Game Design Document này được dùng để:

Định nghĩa rõ **tầm nhìn sản phẩm**.

Chuẩn hóa **core gameplay loop**.

Mô tả các hệ thống gameplay chính.

Làm tài liệu tham chiếu cho code, balancing, UI, art, audio và QA.

Hỗ trợ mở rộng game trong tương lai mà không làm vỡ kiến trúc hiện tại.

**2. Project Overview**

**2.1 Bối cảnh dự án**

Trong bối cảnh thị trường game mobile ngày càng cạnh tranh và yêu cầu cao về hiệu năng cũng như khả năng mở rộng, **True Gate ?** được xây dựng với mục tiêu không chỉ dừng lại ở một sản phẩm thử nghiệm, mà hướng tới một sản phẩm có thể phát hành thực tế.

Trò chơi thuộc thể loại **Survival Auto Shooter**, kết hợp với cơ chế **Gate Upgrade**, nơi người chơi điều khiển nhân vật di chuyển theo phương ngang, trong khi nhân vật tự động tấn công liên tục về phía trước.

Kẻ địch được sinh ra liên tục với độ khó tăng dần theo thời gian, tạo ra áp lực gameplay ngày càng cao. Người chơi vừa phải né tránh kẻ địch, vừa tối ưu hóa sức mạnh thông qua việc lựa chọn và đi qua các cổng có hiệu ứng tăng, giảm hoặc nhân sức mạnh.

**2.2 Mục tiêu sản phẩm**

True Gate ? hướng tới việc trở thành một game mobile có thể phát hành thực tế với các mục tiêu chính:

Gameplay đơn giản, dễ hiểu, dễ tiếp cận.

Nhịp độ chơi nhanh, phù hợp với mobile session ngắn.

Cơ chế progression rõ ràng, tạo động lực chơi lại.

Hệ thống Gate tạo ra quyết định chiến thuật tức thời.

Kiến trúc codebase rõ ràng, dễ mở rộng.

Hiệu năng ổn định trên thiết bị mobile.

Có khả năng mở rộng thêm enemy, weapon, gate, upgrade và mode mới.

**3. Game Vision**

**3.1 High Concept**

**True Gate ?** là một game mobile survival auto shooter, nơi người chơi điều khiển nhân vật chibi pixel né tránh làn sóng kẻ địch, tự động bắn về phía trước và liên tục lựa chọn các cổng nâng cấp để gia tăng sức mạnh nhằm sống sót càng lâu càng tốt.

**3.2 Game Pillars**

**Pillar 1 — Survival Pressure**

Người chơi luôn bị đặt dưới áp lực từ số lượng enemy tăng dần theo thời gian. Gameplay phải tạo cảm giác căng thẳng nhưng không hỗn loạn.

**Pillar 2 — Fast Decision-Making**

Gate xuất hiện trong lúc chơi buộc người chơi phải ra quyết định nhanh. Mỗi lựa chọn có thể giúp người chơi mạnh hơn hoặc khiến tình huống trở nên nguy hiểm hơn.

**Pillar 3 — Power Growth**

Người chơi phải cảm nhận rõ nhân vật đang mạnh lên theo thời gian thông qua damage, fire rate, projectile count, range hoặc các chỉ số khác.

**Pillar 4 — Mobile Performance**

Game phải chạy ổn định trên mobile, đặc biệt khi có số lượng lớn enemy, projectile và effect xuất hiện đồng thời.

**4. Target Audience**

**4.1 Nhóm người chơi mục tiêu**

Người chơi mobile casual.

Người thích game sinh tồn ngắn, dễ chơi lại.

Người thích cảm giác nhân vật mạnh dần theo thời gian.

Người thích gameplay đơn giản nhưng có lựa chọn chiến thuật.

**4.2 Session Length**

| **Loại session** | **Thời lượng mục tiêu** |
| --- | --- |
| Short session | 1–3 phút |
| Standard session | 3–7 phút |
| Long session | 7–12 phút |

**5. Core Gameplay**

**5.1 Core Game Loop**

Core loop của True Gate ? xoay quanh ba yếu tố:

**Survival**

Người chơi né tránh enemy.

Enemy tăng dần số lượng và độ nguy hiểm.

Nếu toàn bộ nhân vật bị tiêu diệt, vòng chơi kết thúc.

**Progression**

Người chơi tiêu diệt enemy để ghi điểm.

Nhân vật mạnh dần, hoặc tăng dần số lượng thông qua Gate hoặc upgrade.

Độ khó tăng theo thời gian để giữ nhịp gameplay.

**Decision-Making**

Người chơi lựa chọn đi qua Gate phù hợp.

Một số Gate có thể tăng sức mạnh.

Một số Gate có thể giảm sức mạnh hoặc tạo rủi ro.

Lựa chọn Gate ảnh hưởng trực tiếp tới khả năng sống sót.

**5.2 Moment-to-Moment Gameplay**

Trong mỗi vòng chơi, người chơi sẽ:

Di chuyển nhân vật theo phương ngang.

Né enemy đang tiến tới.

Nhân vật tự động tấn công về phía trước.

Quan sát các Gate xuất hiện.

Chọn Gate phù hợp với tình huống hiện tại.

Tiêu diệt enemy để tăng điểm.

Cố gắng sống sót lâu nhất có thể.

**5.3 Win/Lose Condition**

**Lose Condition**

Người chơi thua khi:

HP của nhân vật về 0.

Nhân vật bị enemy chạm hoặc nhận đủ damage.

Điều kiện kết thúc khác: TBD.

**Win Condition**

Hiện tại game được thiết kế theo dạng endless survival.

Không có điều kiện thắng cố định trong phiên bản hiện tại.

Mục tiêu chính là đạt điểm cao nhất có thể.

Có thể mở rộng thêm milestone hoặc boss wave trong tương lai.

TBD:

Có boss cuối mỗi mốc thời gian hay không.

Có level-based mode hay không.

Có campaign mode hay không.

**6. Player Character**

**6.1 Player Role**

Người chơi điều khiển một nhân vật chibi pixel có khả năng tự động tấn công về phía trước. Người chơi không cần điều khiển việc bắn, mà tập trung vào di chuyển, né tránh và lựa chọn Gate.

**6.2 Player Controls**

| **Input** | **Hành động** |
| --- | --- |
| Swipe / Drag Left | Di chuyển sang trái |
| Swipe / Drag Right | Di chuyển sang phải |
| Auto Fire | Nhân vật tự động tấn công |
| Tap Skill | TBD |

**6.3 Player Stats**

| **Stat** | **Mô tả** | **Giá trị ban đầu** |
| --- | --- | --- |
| HP | Máu của nhân vật | TBD |
| Move Speed | Tốc độ di chuyển ngang | TBD |
| Damage | Sát thương mỗi phát bắn | TBD |
| Fire Rate | Tốc độ bắn | TBD |
| Projectile Count | Số lượng đạn mỗi lần bắn | TBD |
| Attack Range | Tầm bắn | TBD |
| Critical Chance | Tỷ lệ chí mạng | TBD |
| Critical Damage | Sát thương chí mạng | TBD |

**6.4 Player Death**

Khi nhân vật chết:

Gameplay dừng lại.

Enemy và projectile được xử lý hoặc trả về pool.

Màn hình kết quả hiển thị.

Điểm số được tổng kết.

Người chơi có thể chơi lại.

TBD:

Có revive bằng ads hay không.

Có reward sau khi chết hay không.

Có lưu high score local hay cloud hay không.

**7. Combat System**

**7.1 Combat Overview**

Combat được thiết kế theo hướng **auto shooter**, trong đó nhân vật tự động bắn liên tục về phía trước. Người chơi không cần ngắm hoặc nhấn bắn, giúp gameplay phù hợp với mobile và tập trung vào né tránh, positioning và Gate choice.

**7.2 Auto Attack Behavior**

Nhân vật sẽ tự động tấn công khi:

Game đang ở trạng thái Playing.

Player còn sống.

Có thể bắn theo cooldown hiện tại.

Projectile pool còn object khả dụng.

TBD:

Có cần target enemy gần nhất hay chỉ bắn thẳng về phía trước.

Có hỗ trợ multi-direction weapon hay không.

Có weapon đặc biệt hay không.

**7.3 Damage Calculation**

Công thức damage cơ bản:

FinalDamage = BaseDamage * DamageMultiplier + FlatDamageBonus

Nếu có critical:

CriticalDamage = FinalDamage * CriticalDamageMultiplier

TBD:

Có armor/resistance cho enemy hay không.

Có elemental damage hay không.

Có damage over time hay không.

**7.4 Projectile Behavior**

Projectile có thể có các thông số:

| **Property** | **Mô tả** |
| --- | --- |
| Speed | Tốc độ bay |
| Damage | Sát thương |
| Lifetime | Thời gian tồn tại |
| Pierce Count | Số enemy có thể xuyên qua |
| Hit Effect | Hiệu ứng khi va chạm |
| Pool Key | Key dùng cho object pooling |

**8. Enemy System**

**8.1 Enemy Overview**

Enemy được sinh ra liên tục để tạo áp lực sinh tồn. Độ khó tăng dần theo thời gian thông qua số lượng enemy, tốc độ di chuyển, máu, sát thương hoặc pattern spawn.

**8.2 Enemy Types**

| **Enemy Type** | **Mô tả** | **Trạng thái** |
| --- | --- | --- |
| Basic Enemy | Enemy cơ bản, di chuyển thẳng về phía player | Planned |
| Fast Enemy | Enemy tốc độ cao, máu thấp | TBD |
| Tank Enemy | Enemy máu cao, tốc độ thấp | TBD |
| Ranged Enemy | Enemy có khả năng tấn công từ xa | TBD |
| Boss Enemy | Enemy đặc biệt theo mốc thời gian | TBD |

**8.3 Enemy Stats**

| **Stat** | **Mô tả** |
| --- | --- |
| HP | Máu enemy |
| Move Speed | Tốc độ di chuyển |
| Damage | Sát thương gây ra cho player |
| Score Value | Điểm nhận được khi tiêu diệt |
| Spawn Weight | Tỷ lệ xuất hiện |
| Pool Key | Key dùng cho pooling |

**8.4 Enemy Scaling**

V1 chinh thuc dung time-based run progression, khong dung boss/miniboss va khong dung XP level-up screen.

Enemy scaling duoc dieu khien boi `RunProgressionConfig`:

EnemyHP = BaseHP * HpMultiplierCurve
EnemySpeed = BaseSpeed * MoveSpeedMultiplierCurve
EnemyDamage = BaseDamage * DamageMultiplierCurve
EnemyProjectileSpeed = BaseProjectileSpeed * ProjectileSpeedMultiplierCurve
SpawnInterval = SpawnIntervalCurve(TimeSurvived)
MaxActiveEnemies = MaxActiveEnemiesCurve(TimeSurvived)

Enemy mix duoc mo khoa theo moc thoi gian:

Basic melee: unlock 0s, weight cao tu dau run.
Chomboom exploder melee: unlock 45s, weight thap roi tang dan.
Ranged shooter: unlock 75s, weight thap roi tang dan.

Muc tieu la tang ap luc theo kieu survival auto-shooter: dong hon, nhanh hon, trau hon, nhung van doc duoc tren mobile.

**9. Enemy Spawner System**

**9.1 Spawner Goal**

EnemySpawnerSystem chịu trách nhiệm sinh enemy theo thời gian, đảm bảo:

Enemy xuất hiện liên tục.

Độ khó tăng dần.

Không làm giảm hiệu năng.

Có thể cấu hình bằng data.

Có thể mở rộng thêm wave, pattern hoặc boss.

**9.2 Spawn Rules**

Enemy có thể được spawn dựa trên:

Thời gian đã chơi.

Current difficulty level.

Số lượng enemy hiện tại.

Spawn weight của từng enemy type.

Khoảng cách an toàn so với player.

**9.3 Spawn Constraints**

| **Constraint** | **Mô tả** |
| --- | --- |
| Max Active Enemies | Giới hạn enemy đang hoạt động |
| Spawn Interval | Thời gian giữa mỗi lần spawn |
| Spawn Position | Vị trí sinh enemy |
| Safe Zone | Vùng không spawn quá gần player |
| Pool Availability | Chỉ spawn nếu pool còn object |

**9.4 Production Requirement**

EnemySpawnerSystem không nên trực tiếp tạo object bằng Instantiate trong gameplay runtime. Thay vào đó, hệ thống phải lấy enemy từ PoolSystem để giảm GC allocation và đảm bảo hiệu năng mobile.

**10. Gate System**

**10.1 Gate Overview**

Gate là cơ chế chiến thuật chính của game. Trong quá trình chơi, các Gate xuất hiện trên đường di chuyển, mỗi Gate chứa một hiệu ứng tác động trực tiếp tới chỉ số hoặc trạng thái của player.

Người chơi phải lựa chọn Gate phù hợp trong thời gian ngắn, tạo ra yếu tố decision-making liên tục.

**10.2 Gate Types**

| **Gate Type** | **Hiệu ứng** | **Ví dụ** |
| --- | --- | --- |
| Add Gate | Cộng thêm chỉ số | +10 Damage |
| Subtract Gate | Giảm chỉ số | -5 Fire Rate |
| Multiply Gate | Nhân chỉ số | x2 Projectile |
| Divide Gate | Chia chỉ số | /2 Damage |
| Speed Gate | Hiệu ứng đặc biệt | TBD |
|  |  |  |

**10.3 Gate Target Stats**

Gate có thể ảnh hưởng tới:

Damage.

Fire Rate.

Projectile Count.

Move Speed.

HP.

Critical Chance.

Critical Damage.

Weapon behavior.

Score multiplier.

**10.4 Gate Spawn Rules**

Gate có thể spawn theo:

Khoảng thời gian cố định.

Sau khi người chơi đạt số điểm nhất định.

Theo wave.

Theo random weighted rules.

TBD:

Gate xuất hiện đơn lẻ hay theo cặp.

Có bắt buộc chọn một trong hai Gate hay không.

Gate có biến mất sau thời gian nhất định hay không.

**10.5 Gate Balancing Rules**

Để tránh mất cân bằng:

Gate tăng mạnh nên đi kèm rủi ro hoặc hiếm hơn.

Gate nhân chỉ số cần có giới hạn.

Gate giảm chỉ số không nên khiến run thất bại ngay lập tức.

Các chỉ số quan trọng cần có min/max clamp.

Gate effect nên được định nghĩa bằng data để dễ balance.

Ví dụ:

DamageMultiplierMin = 0.25
DamageMultiplierMax = 10.0
ProjectileCountMax = 20
FireRateMax = TBD

**11. Progression System**

**11.1 In-Run Progression**

Progression trong một vòng chơi đến từ:

Gate upgrade.

Score gain.

Enemy kill.

Time survived.

Temporary stat changes.

V1 decision:

Gate la nguon tang tien suc manh chinh cua player trong run.

Khong co XP gem, khong co level-up screen, khong co chon skill sau khi du EXP trong V1.

Enemy kill/score/coin van phuc vu score va meta progression, khong kich hoat in-run level choice.

Combo system de sau V1.

**11.2 Meta Progression**

Meta progression là progression ngoài vòng chơi.

TBD:

Có tiền tệ không.

Có nâng cấp permanent không.

Có unlock character không.

Có unlock weapon không.

Có daily reward không.

**11.3 Score System**

Điểm số có thể được tính từ:

TotalScore = EnemyKillScore + TimeSurvivalScore + BonusScore

TBD:

Công thức tính điểm chính thức.

Có score multiplier hay không.

Có leaderboard hay không.

**12. Game Economy**

**12.1 Economy Status**

Hiện tại economy chưa được xác định đầy đủ.

TBD:

Soft currency.

Hard currency.

Upgrade cost.

Reward per run.

Ads reward.

IAP package.

**12.2 Production Note**

Nếu game có monetization, economy cần được thiết kế cẩn thận để tránh phá vỡ balance cốt lõi. Không nên thêm economy phức tạp trước khi core gameplay ổn định.

**13. Game Modes**

**13.1 Current Mode — Endless Survival**

Chế độ hiện tại là endless survival.

Mục tiêu:

Sống sót lâu nhất có thể.

Tiêu diệt càng nhiều enemy càng tốt.

Tối ưu lựa chọn Gate.

Đạt điểm cao hơn ở mỗi lần chơi.

**13.2 Future Modes**

TBD:

Stage Mode.

Boss Rush.

Challenge Mode.

Daily Run.

Event Mode.

**14. User Interface**

**14.1 Main Menu**

Main Menu cần có các chức năng cơ bản:

Play.

Settings.

Upgrade.

Character.

Shop.

Leaderboard.

TBD:

Có Shop hay không.

Có Character selection hay không.

Có Daily reward hay không.

**14.2 In-Game HUD**

HUD trong gameplay cần hiển thị:

HP.

Score.

Time survived.

Current upgrade stats.

Pause button.

Optional: current weapon info.

**14.3 Result Screen**

Result Screen hiển thị:

Total score.

Time survived.

Enemy killed.

Best score.

Reward earned.

Retry button.

Home button.

TBD:

Có revive button hay không.

Có double reward bằng ads hay không.

**15. Art Direction**

**15.1 Visual Style**

True Gate ? sử dụng phong cách:

Chibi character.

Pixel art.

Màu sắc rõ ràng.

Hiệu ứng dễ đọc trên mobile.

Enemy và projectile phải phân biệt rõ.

**15.2 Readability Rules**

Trong gameplay survival, visual clarity rất quan trọng.

Yêu cầu:

Player phải luôn dễ nhận diện.

Enemy không được bị lẫn với background.

Projectile của player và enemy phải khác nhau rõ ràng.

Gate phải có text/icon dễ hiểu.

Effect không được che mất thông tin quan trọng.

**15.3 Asset List**

TBD:

Player sprite.

Enemy sprite.

Projectile sprite.

Gate sprite.

Background.

VFX.

UI icon.

Button.

Font.

**16. Audio Direction**

**16.1 Audio Goals**

Audio cần hỗ trợ gameplay bằng cách:

Tạo cảm giác hành động nhanh.

Phản hồi rõ khi bắn trúng enemy.

Báo hiệu Gate, hit, death và result.

Không gây mệt khi chơi nhiều lần.

**16.2 Audio List**

TBD:

Main menu music.

Gameplay music.

Player shoot SFX.

Enemy hit SFX.

Enemy death SFX.

Gate collect SFX.

Player damage SFX.

Game over SFX.

Button click SFX.

**17. Technical Design Overview**

**17.1 Technical Goal**

Dự án được xây dựng theo định hướng production-ready, ưu tiên:

Kiến trúc rõ ràng.

Tách biệt trách nhiệm giữa các hệ thống.

Dễ mở rộng.

Dễ debug.

Dễ balance bằng data.

Hiệu năng ổn định trên mobile.

**17.2 Architecture Layers**

Codebase được tổ chức theo các lớp chính:

Core
Gameplay
Systems
Data
UI
Infrastructure

**Core**

Chứa các thành phần nền tảng dùng chung:

Game state.

Event system.

Service locator hoặc dependency injection.

Common utilities.

Base interfaces.

**Gameplay**

Chứa logic gameplay trực tiếp:

Player.

Enemy.

Projectile.

Gate.

Weapon.

Damage handling.

**Systems**

Chứa các hệ thống độc lập:

CombatSystem.

EnemySpawnerSystem.

GateSystem.

PoolSystem.

ScoreSystem.

GameStateSystem.

**Data**

Chứa dữ liệu cấu hình:

Enemy data.

Gate data.

Weapon data.

Player stat data.

Difficulty curve.

Balance config.

**UI**

Chứa các màn hình và HUD:

MainMenuUI.

GameplayHUD.

ResultScreen.

PauseMenu.

SettingsUI.

**Infrastructure**

Chứa phần hỗ trợ kỹ thuật:

Save system.

Analytics.

Ads.

IAP.

Remote config.

Build config.

TBD:

Engine sử dụng.

Unity version.

Render pipeline.

Target device minimum.

**18. Core Systems**

**18.1 CombatSystem**

CombatSystem chịu trách nhiệm:

Quản lý auto attack.

Tạo projectile từ pool.

Tính damage.

Xử lý hit detection.

Gửi event khi enemy bị tiêu diệt.

Không nên chịu trách nhiệm:

Spawn enemy.

Spawn Gate.

Quản lý UI.

Lưu dữ liệu.

**18.2 EnemySpawnerSystem**

EnemySpawnerSystem chịu trách nhiệm:

Spawn enemy theo thời gian.

Áp dụng difficulty scaling.

Chọn enemy type theo weight.

Kiểm tra giới hạn enemy.

Lấy enemy từ PoolSystem.

Không nên chịu trách nhiệm:

Tính damage.

Điều khiển player.

Quản lý score trực tiếp.

**18.3 GateSystem**

GateSystem chịu trách nhiệm:

Spawn Gate.

Chọn Gate effect.

Apply effect lên player stats.

Quản lý thời gian tồn tại của Gate.

Gửi event khi player đi qua Gate.

Không nên chịu trách nhiệm:

Điều khiển movement của player.

Tính score.

Spawn enemy.

**18.4 PoolSystem**

PoolSystem chịu trách nhiệm:

Khởi tạo object pool.

Cấp phát object từ pool.

Trả object về pool.

Theo dõi active/inactive object.

Giảm runtime allocation.

Các object nên dùng pool:

Enemy.

Projectile.

Hit effect.

Damage popup.

Gate.

Coin/reward object nếu có.

**18.5 ScoreSystem**

ScoreSystem chịu trách nhiệm:

Tính điểm từ enemy kill.

Tính điểm theo thời gian sống.

Lưu score hiện tại.

Gửi event cập nhật UI.

Cập nhật high score nếu cần.

**19. Data-Driven Design**

**19.1 Data Philosophy**

Các thông số gameplay nên được đưa ra khỏi code càng nhiều càng tốt để dễ balance.

Ví dụ dữ liệu nên cấu hình được:

Enemy HP.

Enemy speed.

Enemy score value.

Spawn rate.

Gate effect value.

Player base stats.

Weapon stats.

Difficulty curve.

**19.2 Example Enemy Data**

{
  "enemyId": "basic_enemy",
  "displayName": "Basic Enemy",
  "hp": 10,
  "moveSpeed": 2.5,
  "damage": 1,
  "scoreValue": 10,
  "spawnWeight": 100,
  "poolKey": "enemy_basic"
}

**19.3 Example Gate Data**

{
  "gateId": "damage_add_10",
  "displayText": "+10 Damage",
  "operation": "Add",
  "targetStat": "Damage",
  "value": 10,
  "rarity": "Common"
}

**20. Performance Requirements**

**20.1 Performance Target**

Game cần đảm bảo hiệu năng ổn định trên mobile.

| **Metric** | **Target** |
| --- | --- |
| FPS | 60 FPS target |
| Minimum FPS | 30 FPS acceptable |
| Active Enemies | 100–300 |
| Runtime Instantiate | Không dùng trong gameplay chính |
| GC Spike | Hạn chế tối đa |
| Loading Time | TBD |
| Memory Budget | TBD |

**20.2 Optimization Rules**

Sử dụng object pooling cho enemy, projectile và effect.

Tránh Instantiate và Destroy liên tục trong gameplay.

Tránh allocation trong Update loop.

Tách logic update nặng ra khỏi frame nếu cần.

Giới hạn số lượng effect đồng thời.

Sử dụng data cache cho lookup thường xuyên.

Profile thường xuyên trên thiết bị thật.

**21. Save System**

**21.1 Save Data**

TBD:

High score.

Player upgrades.

Unlocked characters.

Unlocked weapons.

Currency.

Settings.

Tutorial progress.

**21.2 Save Requirements**

Save system cần:

Không làm mất dữ liệu người chơi.

Có versioning cho save data.

Có khả năng migrate khi update game.

Có fallback nếu save bị lỗi.

**22. Analytics**

**22.1 Analytics Goals**

Analytics dùng để hiểu hành vi người chơi và cải thiện balance.

TBD:

Tool analytics sử dụng.

Event naming convention.

Privacy requirements.

**22.2 Suggested Events**

| **Event** | **Mục đích** |
| --- | --- |
| game_start | Người chơi bắt đầu run |
| game_end | Run kết thúc |
| enemy_killed | Theo dõi kill count |
| gate_selected | Theo dõi Gate được chọn |
| player_death | Xác định nguyên nhân chết |
| score_reached | Theo dõi điểm số |
| session_length | Đo thời lượng chơi |

**23. Monetization**

**23.1 Monetization Status**

TBD.

Các hướng có thể cân nhắc:

Rewarded ads.

Cosmetic skins.

Remove ads.

Starter pack.

Battle pass hoặc mission pass.

**23.2 Production Note**

Monetization không nên phá core gameplay. Các tính năng kiếm tiền cần được thiết kế sau khi gameplay chính đã đủ vui và ổn định.

**24. QA Plan**

**24.1 QA Goals**

QA cần đảm bảo:

Game không crash trong session dài.

Enemy spawn ổn định.

Gate effect hoạt động đúng.

PoolSystem không bị leak object.

Score được tính đúng.

UI hiển thị chính xác.

Game chạy ổn trên thiết bị mục tiêu.

**24.2 Test Cases**

| **Test Case** | **Expected Result** |
| --- | --- |
| Start game | Player vào gameplay bình thường |
| Player auto shoots | Projectile được bắn liên tục |
| Enemy spawn | Enemy xuất hiện đúng rule |
| Enemy killed | Enemy biến mất và score tăng |
| Player hit | HP giảm đúng |
| Player death | Game over được trigger |
| Gate collected | Stat thay đổi đúng |
| Pool reuse | Object được tái sử dụng |
| High enemy count | FPS vẫn ổn định |

**25. Production Roadmap**

**25.1 Milestone 1 — Core Prototype**

Mục tiêu:

Player movement.

Auto shooting.

Basic enemy.

Enemy spawner.

Basic collision.

Game over.

Score.

**25.2 Milestone 2 — Gate Gameplay**

Mục tiêu:

Gate spawn.

Gate effect.

Player stat modification.

Basic balancing.

HUD update.

**25.3 Milestone 3 — Production Architecture**

Mục tiêu:

Tách Core, Gameplay, Systems, Data.

Hoàn thiện PoolSystem.

Data-driven config.

GameStateSystem.

Save high score.

**25.4 Milestone 4 — Content Expansion**

Mục tiêu:

Thêm enemy type.

Thêm Gate type.

Thêm weapon behavior.

Thêm VFX/SFX.

Thêm UI flow.

**25.5 Milestone 5 — Mobile Optimization**

Mục tiêu:

Profile trên thiết bị thật.

Tối ưu enemy count.

Tối ưu projectile.

Tối ưu effect.

Giảm GC allocation.

**25.6 Milestone 6 — Release Candidate**

Mục tiêu:

QA checklist.

Bug fixing.

Balance pass.

Build pipeline.

Store assets.

Final polish.

**26. Risk Management**

**26.1 Technical Risks**

| **Risk** | **Impact** | **Mitigation** |
| --- | --- | --- |
| Quá nhiều enemy gây tụt FPS | High | Object pooling, spawn cap, profiling |
| Projectile quá nhiều | High | Pooling, limit projectile count |
| Gate effect phá balance | Medium | Clamp stat, data-driven balance |
| Code phụ thuộc chéo | High | Tách system rõ ràng |
| Save data lỗi khi update | Medium | Save versioning |

**26.2 Design Risks**

| **Risk** | **Impact** | **Mitigation** |
| --- | --- | --- |
| Gameplay lặp lại nhanh chán | High | Thêm enemy, Gate, weapon variation |
| Gate choice không có ý nghĩa | High | Thiết kế trade-off rõ |
| Difficulty tăng quá gắt | Medium | Difficulty curve theo data |
| Mobile control khó chịu | High | Test nhiều device, đơn giản hóa input |

**27. Open Questions**

Các mục cần quyết định sau:

Game dùng Unity hay engine khác?

Player chỉ di chuyển ngang hay có thể di chuyển tự do?

Auto attack bắn thẳng hay target enemy?

Gate xuất hiện theo cặp hay theo hàng nhiều lựa chọn?

Có boss không?

Có meta progression không?

Có monetization không?

Có leaderboard không?

Có cloud save không?

Art style pixel kích thước bao nhiêu?

Target device minimum là gì?

**28. Production Checklist**

**Gameplay**

- [ ]  

Player movement hoàn chỉnh.

- [ ]  

Auto attack ổn định.

- [ ]  

Enemy spawn ổn định.

- [ ]  

Gate system hoạt động đúng.

- [ ]  

Score system hoạt động đúng.

- [ ]  

Game over flow hoàn chỉnh.

**Technical**

- [ ]  

Core architecture rõ ràng.

- [ ]  

Systems tách biệt trách nhiệm.

- [ ]  

Object pooling hoàn chỉnh.

- [ ]  

Data-driven config.

- [ ]  

Không dùng Instantiate/Destroy liên tục trong gameplay.

- [ ]  

Profile trên mobile device thật.

**Content**

- [ ]  

Basic enemy.

- [ ]  

Ít nhất 3 loại Gate.

- [ ]  

Basic projectile.

- [ ]  

Basic VFX.

- [ ]  

Basic SFX.

- [ ]  

UI gameplay đầy đủ.

**Release**

- [ ]  

QA pass.

- [ ]  

Balance pass.

- [ ]  

Build Android.

- [ ]  

Build iOS nếu cần.

- [ ]  

Store metadata.

- [ ]  

Privacy policy nếu có analytics/ads.

- [ ]  

Final release candidate.

**29. Current Production Priority**

Ở giai đoạn hiện tại, ưu tiên nên là:

**Chốt core gameplay loop**

**Làm Player + Auto Attack thật chắc**

**Làm EnemySpawnerSystem có pooling**

**Làm GateSystem data-driven**

**Tách architecture sớm**

**Profile hiệu năng ngay từ đầu**

**Chưa vội thêm monetization hoặc meta progression phức tạp**

True Gate ? được định hướng là một game mobile survival auto shooter có khả năng phát hành thực tế. Điểm mạnh cốt lõi của game nằm ở sự kết hợp giữa gameplay sinh tồn, auto shooting và lựa chọn Gate theo thời gian thực.

Để đạt chuẩn production, dự án cần được xây dựng với kiến trúc rõ ràng, hệ thống gameplay tách biệt, dữ liệu dễ cấu hình và hiệu năng ổn định trên mobile. Các mục chưa hoàn thiện có thể để TBD, nhưng cấu trúc document và codebase cần được thiết kế ngay từ đầu theo hướng dễ mở rộng, dễ bảo trì và dễ kiểm soát chất lượng.
