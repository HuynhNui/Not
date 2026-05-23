# 📋 True Gate? - Code Flow Guide

**Hướng dẫn lưu loát code cho nhà phát triển**

---

## 📁 Cấu Trúc Thư Mục

```
Assets/_Project/Scripts/
├── Core/                           # Nền tảng game
│   ├── GameLoop/
│   │   └── GameManager.cs         # Entry point, quản lý toàn bộ game
│   └── StateMachine/
│       └── GameStateMachine.cs    # Quản lý trạng thái game
│
├── Gameplay/                        # Logic gameplay trực tiếp
│   ├── Player/
│   │   ├── PlayerController.cs    # Điều khiển đội chơi (shoot + movement)
│   │   ├── PlayerUnit.cs          # Base class cho player unit
│   │   ├── MainPlayerUnit.cs      # Player chính (có máu, có sự kiện chết)
│   │   ├── FollowerUnit.cs        # Các unit theo dõi (có thể thêm sau)
│   │   └── PlayerMovement.cs      # Xử lý input + di chuyển player
│   │
│   ├── Combat/
│   │   ├── WeaponController.cs    # Quản lý vũ khí (chưa hoàn thành)
│   │   ├── BulletSpawner.cs       # Sinh đạn qua pool
│   │   ├── Bullet.cs             # Logic đạn (va chạm, lây lên)
│   │   └── *Modifier.cs          # Modifier cho đạn (pierce, homing, split)
│   │
│   ├── Enemies/
│   │   └── EnemyController.cs     # Logic enemy (di chuyển, nhận damage)
│   │
│   ├── Gates/
│   │   ├── GateLogic.cs           # Logic cửa (di chuyển xuống, va chạm)
│   │   ├── GateEffectApplier.cs   # Apply hiệu ứng cửa lên player
│   │   ├── GateTrigger.cs         # Trigger kích hoạt cửa
│   │   └── DoorView.cs            # Hiển thị cửa
│   │
│   └── Units/
│       └── UnitMovement.cs        # Base class cho movement
│
├── Systems/                         # Hệ thống độc lập
│   ├── CombatSystem/
│   │   └── CombatSystem.cs        # Quản lý combat (chưa hoàn thành)
│   │
│   ├── EnemySpawnerSystem/
│   │   └── EnemySpawnerSystem.cs  # Sinh enemy theo thời gian + difficulty
│   │
│   ├── GateSystem/
│   │   └── GateSystem.cs          # Sinh gate, quản lý lựa chọn player
│   │
│   ├── PoolSystem/
│   │   └── PoolSystem.cs          # Object pooling cho bullets, enemies, gates
│   │
│   ├── LevelSystem/
│   │   └── LevelSystem.cs         # Quản lý level
│   │
│   ├── RunStatsSystem/
│   │   └── RunStatsTracker.cs     # Theo dõi thống kê run
│   │
│   └── UISystem/
│       └── UISystem.cs            # Quản lý UI
│
├── Data/                            # Dữ liệu cấu hình
│   └── ScriptableObjects/
│       ├── PlayerConfigs/
│       │   └── PlayerUnitConfig.cs
│       ├── SpawnConfigs/
│       │   ├── EnemySpawnConfig.cs
│       │   └── EnemyArchetypeData.cs
│       └── GateConfigs/
│           └── GateConfig.cs
│
├── Interfaces/                      # Interface dùng chung
│   ├── IPoolable.cs
│   ├── IDamageable.cs
│   └── IGateEffect.cs
│
└── Utils/
    └── MathHelper.cs
```

---

## 🎮 Game State Machine

### Các trạng thái:
```csharp
public enum GameState
{
    Bootstrap,    // Khởi động
    Playing,      // Đang chơi
    Paused,       // Tạm dừng
    GameOver      // Kết thúc
}
```

### Chuyển tiếp:
```
Bootstrap → Playing → (Paused ↔ Playing)
                  ↓
              GameOver
```

---

## 🚀 Game Initialization Flow

```
Scene Load
    ↓
GameManager.Awake()
    ↓
GameManager.Init()
    ├─ UISystem.Init()
    ├─ GateSystem.Init()
    │  ├─ Resolve Camera
    │  ├─ Find PoolSystem
    │  ├─ Find PlayerController
    │  └─ Find MainPlayerUnit
    │
    ├─ Find PlayerController (nếu chưa assign)
    ├─ Find MainPlayerUnit (nếu chưa assign)
    └─ Subscribe MainPlayerUnit.Died event
        
EnemySpawnerSystem.Awake()
    ├─ Init()
    └─ Bắt đầu spawn enemy

PlayerController.Awake()
    ├─ PlayerMovement.Init()
    ├─ MainPlayerUnit.Initialize()
    └─ (Followers nếu có)
```

---

## ⚔️ Gameplay Loop (Main Update)

### Mỗi frame (60 FPS):

```
Update() được gọi cho tất cả components
    ↓
┌───────────────────────────────────────────────────────┐
│ 1. PlayerMovement.Update()                            │
│    ├─ ReadInput() → Đọc touch/mouse                   │
│    └─ Move() → Cập nhật X position của player        │
│                                                       │
│ 2. PlayerController.Update()                          │
│    └─ ShootSquad() → Gọi Shoot() trên tất cả units   │
│       ├─ MainPlayerUnit.Shoot()                       │
│       │  └─ bulletSpawner.Shoot()                     │
│       │     ├─ CanShoot()? (kiểm tra fireRate)        │
│       │     └─ SpawnBullet() → Lấy từ PoolSystem     │
│       │        ├─ Bullet.Init()                       │
│       │        ├─ Bullet.Configure(modifiers)         │
│       │        └─ Bullet.Spawn()                      │
│       └─ FollowerUnit.Shoot() (nếu có)               │
│                                                       │
│ 3. Bullet.Update()  [cho mỗi bullet trong scene]      │
│    ├─ Cập nhật position                              │
│    ├─ Kiểm tra collision với enemy                    │
│    └─ Apply modifier effects                          │
│                                                       │
│ 4. EnemySpawnerSystem.Update()                        │
│    ├─ _elapsedTime += Time.deltaTime                 │
│    ├─ Kiểm tra GetCurrentSpawnInterval()             │
│    │  └─ Tính từ difficultyCurve                     │
│    └─ Nếu _nextSpawnTime → Spawn()                   │
│       ├─ Lấy spawn position từ camera                │
│       └─ EnemyController.Init() + Spawn()            │
│                                                       │
│ 5. EnemyController.Update() [cho mỗi enemy]          │
│    ├─ MoveTowardsTarget() → Di chuyển về player     │
│    └─ DespawnIfOutOfBounds()                          │
│                                                       │
│ 6. GateSystem.Update()                                │
│    ├─ Kiểm tra enemy/player chưa chết                │
│    ├─ Kiểm tra Time.time >= _nextSpawnTime           │
│    └─ Nếu → Spawn() [3 cửa mới]                      │
│       ├─ PickGateConfig() [lựa chọn 3 config]        │
│       ├─ GetLaneWorldX() [tính vị trí X]             │
│       └─ GateLogic.Init() + Spawn()                  │
│                                                       │
│ 7. GateLogic.Update() [cho mỗi gate]                 │
│    ├─ MoveStraightDown()                              │
│    └─ DespawnIfOutOfBounds()                          │
│                                                       │
│ 8. Player collision events                            │
│    ├─ Bullet va chạm Enemy                            │
│    │  └─ Enemy.TakeDamage()                           │
│    │     ├─ currentHealth -= damage                   │
│    │     ├─ RefreshHealthBar()                        │
│    │     └─ Nếu chết → Despawn() + EnemyKilled event │
│    │                                                   │
│    ├─ Player va chạm Gate                             │
│    │  └─ GateTrigger.OnTriggerStay()                  │
│    │     └─ GateLogic.HandlePlayerTriggered()         │
│    │        └─ GateSystem.HandleGateChosen()          │
│    │           ├─ GateLogic.ApplyEffect()             │
│    │           ├─ GateEffectApplier.Apply()           │
│    │           │  ├─ Tùy theo config.StatTarget       │
│    │           │  └─ ApplyToSquadDamage/FireRate/etc  │
│    │           └─ Despawn() các gate khác             │
│    │                                                   │
│    └─ Enemy va chạm Player                            │
│       └─ MainPlayerUnit.TakeDamage()                  │
│          ├─ currentHealth -= damage                   │
│          ├─ RefreshHealthBar()                        │
│          └─ Nếu chết → Die()                          │
│             └─ GameManager.HandlePlayerDied()        │
│                ├─ PlayerController.SetControlsEnabled(false)
│                ├─ EnemySpawnerSystem.SetSpawningEnabled(false)
│                └─ UISystem.ShowGameOver()             │
└───────────────────────────────────────────────────────┘
```

---

## 🔄 Shooting Flow (Chi tiết)

### PlayerController.ShootSquad() được gọi từ Update():

```python
PlayerController.ShootSquad()
    │
    ├─ if (!_controlsEnabled) → return
    │
    ├─ MainPlayerUnit.Shoot()
    │   └─ if (bulletSpawner == null) → return
    │       └─ bulletSpawner.Shoot()
    │           │
    │           ├─ CanShoot()? 
    │           │  └─ Kiểm tra:
    │           │     ├─ bulletPrefab != null
    │           │     ├─ fireRate > 0f
    │           │     └─ Time.time >= _nextShotTime
    │           │
    │           ├─ Tính spawnPosition từ firePoint
    │           ├─ Lặp tạo projectile_count đạn:
    │           │  └─ SpawnBullet(position, damage, modifiers)
    │           │     │
    │           │     ├─ poolSystem.Spawn()
    │           │     │  └─ Lấy object từ pool
    │           │     │     └─ Nếu không còn → tạo mới
    │           │     │
    │           │     ├─ bullet.Init(damage, bulletSpeed)
    │           │     ├─ bullet.Configure(modifiers)
    │           │     │  └─ Thêm pierce, homing, split, etc.
    │           │     └─ bullet.Spawn()
    │           │        ├─ Kích hoạt collider
    │           │        └─ Bắt đầu di chuyển
    │           │
    │           └─ _nextShotTime = Time.time + (1f / fireRate)
    │
    └─ Lặp qua followers:
        └─ follower.Shoot() [nếu có]
```

### Mỗi Bullet trong Update:

```python
Bullet.Update()
    │
    ├─ Kiểm tra _isActive
    ├─ Cập nhật position: velocity * Time.deltaTime
    ├─ Kiểm tra lifetime → Despawn nếu quá hạn
    │
    └─ OnTriggerEnter2D(enemy)
       │
       ├─ enemy.TakeDamage(damage)
       │  ├─ Cập nhật HP
       │  ├─ Refresh health bar
       │  └─ Nếu HP ≤ 0 → Die()
       │     └─ EnemySpawnerSystem.NotifyEnemyKilled(this)
       │        └─ Trigger event EnemyKilled
       │           └─ RunStatsTracker ghi nhận điểm số
       │
       └─ ApplyModifier effects (pierce, split, etc.)
```

---

## 👾 Enemy Spawner Flow

### EnemySpawnerSystem.Update():

```
Kiểm tra điều kiện spawn:
    ├─ _spawningEnabled?
    ├─ enemyPrefab != null?
    ├─ playerUnit != null?
    └─ playerUnit.IsDead?

_elapsedTime += Time.deltaTime

Nếu Time.time >= _nextSpawnTime:
    │
    ├─ Spawn()
    │  │
    │  ├─ GetSpawnPosition()
    │  │  └─ Từ camera viewport hoặc spawnPoints[]
    │  │
    │  ├─ poolSystem.Spawn(enemy, position, rotation)
    │  │  └─ Lấy từ pool hoặc tạo mới
    │  │
    │  ├─ EnemyController.Init()
    │  │  ├─ _target = player.transform
    │  │  ├─ currentHealth = maxHealth
    │  │  └─ EnsureHealthBar()
    │  │
    │  ├─ EnemyController.Spawn()
    │  │  └─ _isActive = true
    │  │
    │  └─ Thêm vào scene
    │
    └─ _nextSpawnTime = Time.time + GetCurrentSpawnInterval()
       │
       └─ GetCurrentSpawnInterval()
          ├─ activeDifficultyCurve.Evaluate(_elapsedTime)
          │  └─ Từ AnimationCurve (tăng theo thời gian)
          │
          ├─ scaledInterval = baseInterval / difficultyMultiplier
          └─ return Mathf.Max(minimumInterval, scaledInterval)
```

---

## 🚪 Gate System Flow

### GateSystem.Update():

```
Kiểm tra:
    ├─ mainPlayerUnit != null
    ├─ !mainPlayerUnit.IsDead
    ├─ !_isGateSetActive (chỉ spawn nếu không có gate active)
    └─ Time.time >= _nextSpawnTime

Nếu đủ điều kiện → Spawn()
    │
    ├─ ClearActiveGates()
    │  └─ Despawn tất cả gate cũ
    │
    ├─ _isGateSetActive = true
    ├─ _choiceLocked = false
    │
    ├─ Lặp gateCount lần (thường 3):
    │  │
    │  ├─ GateConfig config = PickGateConfig(index)
    │  │  ├─ Chọn random từ availableGateConfigs[]
    │  │  └─ Cố gắng tránh trùng lặp
    │  │
    │  ├─ float laneX = GetLaneWorldX(index, gateCount)
    │  │  ├─ Chia đều theo viewport hoặc laneSpacing
    │  │  └─ Tính viewport position → world position
    │  │
    │  ├─ Vector3 spawnY = GetSpawnWorldY()
    │  │  └─ Phía trên camera + offset
    │  │
    │  ├─ poolSystem.Spawn(gate, position, Quaternion.identity)
    │  │
    │  ├─ GateLogic.Init(config, this, mainUnit, controller, camera, pool, laneX)
    │  │  ├─ _gateSystem = this
    │  │  ├─ _lockedLaneWorldX = laneX
    │  │  └─ doorView.Bind(config)
    │  │
    │  ├─ GateLogic.Spawn()
    │  │  └─ _isActive = true
    │  │
    │  └─ activeGates.Add(instance)
    │
    └─ _nextSpawnTime = Time.time + spawnIntervalSeconds
```

### GateLogic.Update():

```
Mỗi frame (nếu _isActive):
    │
    ├─ MoveStraightDown()
    │  ├─ nextY = position.y - moveSpeed * Time.deltaTime
    │  └─ _rigidbody.MovePosition(new Vector2(laneX, nextY))
    │
    └─ DespawnIfOutOfBounds()
       └─ Nếu y < camera.bottom - offset → Despawn()
```

### GateTrigger.OnTriggerStay2D(player):

```
Player đi qua Gate:
    │
    └─ GateLogic.HandlePlayerTriggered()
       └─ GateSystem.HandleGateChosen(this)
          │
          ├─ Kiểm tra !_choiceLocked
          ├─ _choiceLocked = true
          │
          ├─ this.ApplyEffect()
          │  └─ GateEffectApplier.Apply(config, mainUnit, controller)
          │     │
          │     └─ Tùy theo config.StatTarget:
          │        ├─ Damage
          │        │  └─ newDamage = ApplyOperation(current, operation, amount)
          │        │     └─ mainUnit.SetDamage(newDamage)
          │        │        └─ bulletSpawner.SetDamage()
          │        │
          │        ├─ FireRate
          │        │  └─ newFireRate = ApplyOperation(...)
          │        │     └─ mainUnit.SetFireRate()
          │        │        └─ bulletSpawner.SetFireRate()
          │        │
          │        ├─ MaxHp
          │        │  └─ mainUnit.SetMaxHp(newMaxHp)
          │        │     └─ currentHp cập nhật theo
          │        │
          │        └─ ProjectileCount
          │           └─ bulletSpawner.SetProjectileCount()
          │
          ├─ Despawn tất cả gate khác
          │  └─ gate.Despawn() (trừ gate được chọn)
          │
          ├─ Nếu config.ConsumeAfterUse:
          │  └─ this.Despawn()
          │     └─ poolSystem.Release(this)
          │
          └─ _isGateSetActive = false
```

### Các Operation Type:

```csharp
GateOperationType.Add       → newValue = baseValue + amount
GateOperationType.Subtract  → newValue = Mathf.Max(0, baseValue - amount)
GateOperationType.Multiply  → newValue = baseValue * amount
GateOperationType.Divide    → newValue = baseValue / amount
```

---

## 💥 Collision & Damage Flow

### Enemy va chạm Bullet:

```
Bullet.OnTriggerEnter2D(enemy)
    │
    ├─ enemy.TakeDamage(bulletDamage)
    │  │
    │  ├─ _currentHp -= damage
    │  ├─ RefreshHealthBar() → Update UI
    │  │
    │  └─ Nếu _currentHp ≤ 0:
    │     │
    │     └─ Die()
    │        ├─ _isDead = true
    │        ├─ poolSystem.Release(this)
    │        └─ EnemySpawnerSystem.NotifyEnemyKilled(this)
    │           └─ Trigger EnemyKilled event
    │              └─ RunStatsTracker.RecordKill()
    │
    └─ Bullet modifier effects:
       ├─ Pierce: Xuyên qua nhiều enemy
       ├─ Homing: Hướng về enemy gần nhất
       └─ Split: Tách thành nhiều đạn bé
```

### Enemy va chạm Player:

```
EnemyController đặt OnTriggerStay2D:
    │
    └─ MainPlayerUnit.TakeDamage(enemyDamage)
       │
       ├─ _currentHp -= damage
       ├─ RefreshHealthBar()
       │
       └─ Nếu _currentHp ≤ 0:
          │
          └─ Die()
             ├─ _isDead = true
             ├─ Died?.Invoke(this)
             │  └─ GameManager.HandlePlayerDied()
             │     ├─ playerController.SetControlsEnabled(false)
             │     ├─ enemySpawnerSystem.SetSpawningEnabled(false)
             │     └─ uiSystem.ShowGameOver()
             │
             └─ Game Over!
```

---

## 🎯 Object Pooling

### PoolSystem quản lý:

```
Pool mỗi loại object:

┌─────────────────────────────────────┐
│ Bullet Pool                         │
├─────────────────────────────────────┤
│ Prefab: BulletPrefab                │
│ Active: [Bullet_0, Bullet_1, ...]   │
│ Inactive: [Bullet_10, Bullet_11, ...]
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Enemy Pool                          │
├─────────────────────────────────────┤
│ Prefab: EnemyPrefab                 │
│ Active: [Enemy_0, Enemy_1, ...]     │
│ Inactive: [Enemy_5, Enemy_6, ...]   │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Gate Pool                           │
├─────────────────────────────────────┤
│ Prefab: GatePrefab                  │
│ Active: [Gate_0, Gate_1, Gate_2]    │
│ Inactive: [Gate_3, ...]             │
└─────────────────────────────────────┘
```

### Spawn từ Pool:

```
poolSystem.Spawn(prefab, position, rotation)
    │
    ├─ Kiểm tra pool có object inactive?
    ├─ Nếu có → Lấy ra + SetActive(true)
    └─ Nếu không → Instantiate mới

Release về Pool:

poolSystem.Release(object)
    │
    ├─ Thêm vào inactive list
    ├─ SetActive(false)
    └─ Reset state (position, rotation, velocity)
```

---

## 📊 Data-Driven Configuration

### GateConfig ScriptableObject:

```csharp
public class GateConfig
{
    public string gateId;                    // "damage_add_10"
    public string displayName;               // "+10 Damage"
    public GateStatTarget statTarget;        // Damage, FireRate, MaxHp, ...
    public GateOperationType operationType;  // Add, Subtract, Multiply, Divide
    public float amount;                     // 10.0f
    public string description;               // Mô tả
}

// Ví dụ:
// gateId: "damage_multiply_2"
// displayName: "x2 Damage"
// statTarget: Damage
// operationType: Multiply
// amount: 2.0f
```

### EnemySpawnConfig ScriptableObject:

```csharp
public class EnemySpawnConfig
{
    public AnimationCurve difficultyCurve;  // Tính difficulty theo thời gian
    public float baseSpawnInterval;          // Thời gian spawn ban đầu
    public float minimumSpawnInterval;       // Thời gian spawn tối thiểu
    public float spawnYOffset;               // Độ cao spawn
    public float horizontalSpawnPadding;     // Padding ngang
}

// Difficulty curve ví dụ:
// t=0s → difficulty=1.0x (spawn 1 enemy/1.5s)
// t=60s → difficulty=2.5x (spawn 1 enemy/0.6s)
```

### PlayerUnitConfig ScriptableObject:

```csharp
public class PlayerUnitConfig
{
    public float damage;
    public float fireRate;
    public float maxHealth;
    public float bulletSpeed;
    public int projectileCount;
}
```

---

## 🔧 Key Components Explained

### PlayerController (Điều khiển đội chơi)

**Trách nhiệm:**
- Quản lý MainPlayerUnit + Followers
- Gọi Shoot() mỗi frame
- Xử lý Gate effect lên cả đội

**Quan trọng:**
- `mainPlayerUnit` phải được assign trong Inspector
- `autoFire = true` → Tự động bắn trong Update

### BulletSpawner (Sinh đạn)

**Trách nhiệm:**
- Kiểm tra fireRate cooldown
- Spawn bullet từ PoolSystem
- Áp dụng modifier (pierce, homing, split)

**Phương thức chính:**
- `Shoot()` → Sinh projectile_count đạn
- `Initialize(damage, fireRate)` → Cài đặt stats
- `SpawnBullet()` → Lấy từ pool + init

### EnemySpawnerSystem (Sinh enemy)

**Trách nhiệm:**
- Spawn enemy theo thời gian
- Tính difficulty curve
- Gọi EnemyKilled event

**Quan trọng:**
- `difficultyCurve` ảnh hưởng spawn rate
- `baseSpawnInterval` -> `minimumSpawnInterval`
- _elapsedTime dùng để evaluate curve

### GateSystem (Quản lý cửa)

**Trách nhiệm:**
- Spawn 3 gate mỗi 20s
- Quản lý player choice
- Gọi GateEffectApplier

**Trạng thái:**
- `_isGateSetActive` → True nếu có gate đang chờ chọn
- `_choiceLocked` → True sau khi player chọn gate

### GateEffectApplier (Áp dụng hiệu ứng)

**Phương thức:**
```csharp
Apply(config, mainUnit, squad)
    → Tùy config.StatTarget gọi:
       - ApplyToSquadDamage()
       - ApplyToSquadFireRate()
       - ApplyMaxHp()
       - ApplyProjectileCount()
```

---

## ⚙️ Difficulty Scaling

### AnimationCurve Difficulty:

```
Difficulty Multiplier
      ↑
    3.0 ├─────────────── (t=120s)
        │    ╱╱╱
    2.5 ├──╱╱╱╱
        │ ╱╱╱╱╱
    2.0 ├╱╱╱╱
        │
    1.5 ├
        │
    1.0 ├─ (t=0s)
        │
        └─────────────────→ Time
          0   30   60  120
        
Formula:
    difficultyMultiplier = curve.Evaluate(_elapsedTime)
    spawnInterval = baseInterval / difficultyMultiplier
    
Ví dụ:
    t=0s   → mult=1.0x  → interval = 1.5s / 1.0 = 1.5s
    t=30s  → mult=1.5x  → interval = 1.5s / 1.5 = 1.0s
    t=60s  → mult=2.0x  → interval = 1.5s / 2.0 = 0.75s
    ...
    t=120s → mult=2.5x  → interval = 1.5s / 2.5 = 0.6s
    
Final: max(minimumInterval, interval) = max(0.35s, result)
```

---

## 🛠️ Common Debug / Extension Points

### Thêm loại Gate mới:

```
1. Tạo GateConfig ScriptableObject
   ├─ gateId: "new_gate_id"
   ├─ statTarget: NewStat
   └─ operationType: Add/Multiply/etc
   
2. Thêm vào GateSystem.availableGateConfigs[]
3. (Optional) Thêm case mới trong GateEffectApplier.Apply()
```

### Thêm loại Enemy mới:

```
1. Tạo EnemyController variant
   ├─ Inherit từ EnemyController
   └─ Override MoveTowardsTarget() nếu cần
   
2. Tạo Prefab
3. Thêm vào PoolSystem poolDefinitions[]
4. Assign trong EnemySpawnerSystem.enemyPrefab
```

### Thêm Bullet Modifier:

```
1. Inherit từ IBulletModifier
2. Implement OnSpawn(), OnTick(), OnHit()
3. Tạo Config ScriptableObject
   ├─ modifier class reference
   └─ configuration values
   
4. Thêm vào BulletSpawner.defaultModifierConfigs[]
```

---

## 📌 Important Notes

❗ **MainPlayerUnit phải được assign**:
- PlayerController cần mainPlayerUnit reference
- GameManager cần mainPlayerUnit reference  
- Nếu không assign → FindAnyObjectByType() được gọi

❗ **PoolSystem phải được setup**:
- Bullet, Enemy, Gate cần pool definitions
- poolDefinitions[].poolId phải unique
- initialSize có thể = 0 (dynamic spawn)

❗ **GateConfig phải có**:
- statTarget hợp lệ
- operationType hợp lệ
- amount > 0 (abs được lấy)

❗ **Gravity Scale = 0 + Freeze Y**:
- Player Rigidbody2D: m_GravityScale: 0
- m_Constraints: 2 (Freeze Y position)

❗ **gameplayCamera phải là ortho**:
- Camera.orthographic = true
- Rất nhiều tính toán dựa trên orthographicSize

---

## 🎯 Testing Checklist

- [ ] Player shoot được
- [ ] Enemy spawn và di chuyển
- [ ] Bullet va chạm enemy → enemy chết
- [ ] Enemy va chạm player → player bị damage
- [ ] Gate spawn và di chuyển
- [ ] Player chạm gate → effect áp dụng
- [ ] Difficulty tăng theo thời gian
- [ ] Game Over khi player HP = 0
- [ ] PoolSystem tái sử dụng object
- [ ] Không có memory leak (GC Allocations)

---

**Document Version**: v1.0  
**Last Updated**: May 21, 2026  
**Author**: Code Flow Guide


