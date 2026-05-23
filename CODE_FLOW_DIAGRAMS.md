# 🔀 True Gate? - Detailed Flow Diagrams

**Chi tiết luồng code với ASCII diagrams**

---

## 1️⃣ STARTUP FLOW

```
Game Start
    │
    ├─→ Unity Scene Load
    │   └─→ All Awake() called
    │
    ├─→ GameManager.Awake()
    │   │
    │   └─→ GameManager.Init()
    │       ├─→ UISystem.Init()
    │       ├─→ GateSystem.Init()
    │       │   ├─ ResolveGameplayCamera()
    │       │   ├─ FindAnyObjectByType<PoolSystem>()
    │       │   ├─ FindAnyObjectByType<PlayerController>()
    │       │   └─ FindAnyObjectByType<MainPlayerUnit>()
    │       ├─→ Find PlayerController (if not assigned)
    │       ├─→ Find MainPlayerUnit (if not assigned)
    │       └─→ Subscribe to MainPlayerUnit.Died event
    │
    ├─→ EnemySpawnerSystem.Awake()
    │   └─→ EnemySpawnerSystem.Init()
    │       └─ Reset timers
    │
    ├─→ PlayerController.Awake()
    │   ├─→ PlayerMovement.Init()
    │   │   └─ Cache camera, bounds collider
    │   └─→ MainPlayerUnit.Initialize()
    │       ├─ ApplyUnitConfig()
    │ ┌―――├─ SyncSpawnerStats()
    │ │   └─ bulletSpawner.Initialize(damage, fireRate)
    │ │
    │ └─→ MainPlayerUnit Status: READY
    │
    └─→ Game State: PLAYING
```

---

## 2️⃣ MAIN GAMEPLAY TICK (Per Frame)

```
═════════════════════════════════════════════════════════════
                        FRAME UPDATE
═════════════════════════════════════════════════════════════

    ┌─────────────────────────────────────────────────────┐
    │ Update() calls [ALL components]                     │
    └─────────────────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
    [INPUT]        [MOVEMENT]      [GAMEPLAY]
         │               │               │
         │               ▼               │
   Touch/Mouse    PlayerMovement    EnemySpawner
   Input Read     Update            Update
         │               │               │
         │      Read      │      Check     │
         │      Touch   ▼      Spawn      │
         │      Mouse  Update             │
         │              Position    ▼     │
         │              X only   Spawn    │
         │              (Y frozen) Enemies │
         │                  │             │
         └───────────────┬──┴─────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
      [COMBAT]      [GATE]         [COLLISION]
         │               │               │
         │      ▼        │      ▼        │
   PlayerController  GateSystem    Bullet.Update()
   ShootSquad()      Spawn Gates   ├─ Move bullet
         │               │         ├─ Check collision
         │      ▼        │      ▼   │  with Enemy
         ├─→ MainUnit   ├─→ 3 Gates │
         │   Shoot()    │   Spawn    │  Enemy.TakeDamage()
         │   └─ Bullet  │           │
         │     Spawn    │  ▼        │  ▼
         │   from Pool  ├─→ Lane   EnemyController
         │             │  Positions Update()
         │  ▼          │           ├─ Move toward player
   Configure           │  ▼        ├─ Check bounds
   Modifiers:         ├─→ Door    │
   ├─ Pierce          │   Views    │  ▼
   ├─ Homing          │   Bind     Collision with
   ├─ Split           │           Player:
         │             │           ├─ TakeDamage()
         ▼             ▼           ├─ Update HP
    Bullets in       Gates        └─ Check if Dead
    Flight         Moving              │
                  Down Screen           ▼
                                   [IF DEAD]
                                   ├─ Game Over
                                   │  event
                                   └─ Stop spawning
```

---

## 3️⃣ SHOOTING SEQUENCE

```
PlayerController.Update()
    │
    └─→ Is autoFire enabled?
        ├─YES─→ ShootSquad()
        │        │
        │        └─→ For each unit in squad:
        │            │
        │            └─→ Unit.Shoot()
        │                │
        │                ├─ bulletSpawner != null?
        │                │  ├─NO─→ return
        │                │  └─YES─→ bulletSpawner.Shoot()
        │                │          │
        │                │          └─→ CanShoot()?
        │                │              │
        │                │              ├─ bulletPrefab == null?  ─YES─→ return
        │                │              ├─ fireRate <= 0?         ─YES─→ return
        │                │              └─ Time.time < _nextShotTime? ─YES─→ return
        │                │              
        │                │              └─YES all checks pass:
        │                │                  │
        │                │                  ├─→ For i = 0 to projectileCount-1:
        │                │                  │   │
        │                │                  │   └─→ SpawnBullet()
        │                │                  │       │
        │                │                  │       ├─ Calc position (burst spread)
        │                │                  │       │
        │                │                  │       ├─ poolSystem.Spawn(bulletPrefab)
        │                │                  │       │  ├─ Check pool
        │                │                  │       │  ├─ Get or create
        │                │                  │       │  └─ SetActive(true)
        │                │                  │       │
        │                │                  │       ├─ bullet.Init(damage, speed)
        │                │                  │       │
        │                │                  │       ├─ bullet.Configure(modifiers)
        │                │                  │       │  ├─ Add Pierce if configured
        │                │                  │       │  ├─ Add Homing if configured
        │                │                  │       │  └─ Add Split if configured
        │                │                  │       │
        │                │                  │       └─ bullet.Spawn()
        │                │                  │          └─ _isActive = true
        │                │                  │
        │                │                  └─→ _nextShotTime = Time.time + (1 / fireRate)
        │                │
        └─NO─→ return
```

---

## 4️⃣ BULLET LIFECYCLE

```
SPAWN
    │
    ├─→ Bullet.Spawn()
    │   └─ _isActive = true
    │
    ├─→ Bullet.Init(damage, speed)
    │   ├─ _damage = damage
    │   └─ _velocity = speed
    │
    ├─→ Bullet.Configure(modifiers)
    │   ├─ Add PierceModifier?
    │   ├─ Add HomingModifier?
    │   └─ Add SplitModifier?
    │
    ▼
ACTIVE (Every Update)
    │
    ├─→ Bullet.Update()
    │   │
    │   ├─ Is _isActive?
    │   │  └─NO─→ return
    │   │
    │   └─ Move:
    │       position += _velocity * Time.deltaTime
    │
    │   Check collision:
    │       OnTriggerEnter2D(collider)
    │       │
    │       ├─→ Is it an Enemy?
    │       │   ├─YES─→ Enemy.TakeDamage(_damage)
    │       │   │       ├─ currentHealth -= damage
    │       │   │       │
    │       │   │       ├─ RefreshHealthBar()
    │       │   │       │
    │       │   │       ├─ Is currentHealth <= 0?
    │       │   │       │  ├─YES─→ Die()
    │       │   │       │  │       ├─ _isDead = true
    │       │   │       │  │       ├─ poolSystem.Release()
    │       │   │       │  │       └─ EnemySpawner.NotifyEnemyKilled()
    │       │   │       │  └─NO─→ return
    │       │   │
    │       │   ├─ Apply modifiers:
    │       │   │   ├─ Pierce?
    │       │   │   │  └─ Bullet continues (penetrate)
    │       │   │   │
    │       │   │   ├─ Homing?
    │       │   │   │  └─ Find closest enemy, update direction
    │       │   │   │
    │       │   │   └─ Split?
    │       │   │      └─ SpawnChildBullet x N
    │       │   │
    │       │   └─ After hit:
    │       │       ├─ If Pierce? → continue
    │       │       └─ Else → Despawn()
    │       │
    │       └─→ Not enemy? → return
    │
    │   Check lifetime:
    │       (_elapsedTime > _lifetime)?
    │       ├─YES─→ Despawn()
    │       └─NO─→ continue
    │
    ▼
DESPAWN
    └─→ Bullet.Despawn()
        ├─→ _isActive = false
        ├─→ poolSystem.Release(this)
        │   └─ Return to pool
        └─→ SetActive(false)
```

---

## 5️⃣ ENEMY SPAWN & LIFECYCLE

```
ENEMY SPAWNER LIFECYCLE
═════════════════════════════════════════
Every Frame:
    _elapsedTime += Time.deltaTime
    
    Check: Time.time >= _nextSpawnTime?
    
    NO:   continue
    
    YES:  Spawn()
          │
          ├─→ GetSpawnPosition()
          │   ├─ Use spawnPoints[]? (if available)
          │   ├─ Else: random position above camera
          │   └─ Position: (randomX, cameraTop + offset, 0)
          │
          ├─→ poolSystem.Spawn(enemyPrefab, position)
          │   └─ Get from pool or instantiate
          │
          ├─→ EnemyController.Init(target, player, camera)
          │   ├─ _target = player.transform
          │   ├─ _playerUnit = player
          │   ├─ currentHealth = maxHealth
          │   └─ EnsureHealthBar()
          │
          ├─→ EnemyController.Spawn()
          │   ├─ currentHealth = GetMaxHealth()
          │   ├─ _isActive = true
          │   └─ RefreshHealthBar()
          │
          └─→ _nextSpawnTime = Time.time + GetCurrentSpawnInterval()
              │
              └─→ GetCurrentSpawnInterval()
                  ├─ Get AnimationCurve from config
                  ├─ difficultyMultiplier = curve.Evaluate(_elapsedTime)
                  ├─ scaledInterval = baseInterval / difficultyMultiplier
                  └─ return Max(minimumInterval, scaledInterval)
    
═════════════════════════════════════════

ENEMY ACTIVE LIFECYCLE
═════════════════════════════════════════
Every Frame:
    
    EnemyController.Update()
    │
    ├─ Is _isActive?
    │  └─NO─→ return
    │
    ├─ MoveTowardsTarget()
    │  │
    │  ├─ direction = (_target.position - this.position).normalized
    │  ├─ position += direction * moveSpeed * Time.deltaTime
    │  │
    │  └─ Optional: clamp X inside camera bounds
    │
    ├─ DespawnIfOutOfBounds()
    │  │
    │  ├─ Is below camera?
    │  │  └─YES─→ Despawn()
    │  └─ Keep moving
    │
    └─ OnTriggerStay2D(player)
       │
       └─ Player.TakeDamage(this.contactDamage)

═════════════════════════════════════════

ENEMY DEATH
═════════════════════════════════════════
From Bullet.OnTriggerEnter2D():
    
    Enemy.TakeDamage(bulletDamage)
    │
    ├─ currentHealth -= bulletDamage
    ├─ RefreshHealthBar()
    │
    ├─ Is currentHealth <= 0?
    │  │
    │  ├─YES─→ Die()
    │  │       ├─ _isDead = true
    │  │       ├─ poolSystem.Release(this)
    │  │       └─ _spawnerSystem.NotifyEnemyKilled(this)
    │  │           └─ Trigger EnemyKilled event
    │  │               └─ RunStatsTracker records kill
    │  │
    │  └─NO─→ return
    │
    └─ Enemy is now in pool waiting for reuse

═════════════════════════════════════════
```

---

## 6️⃣ GATE SPAWN & TRIGGER FLOW

```
GATE SYSTEM SPAWN CYCLE
═════════════════════════════════════════
Every Frame:
    
    Is playerUnit dead?           ─YES─→ return
    Is _isGateSetActive?          ─YES─→ return (wait for player choice)
    Time.time >= _nextSpawnTime?  ─NO──→ return
    
    YES: Spawn()
         │
         ├─→ ClearActiveGates()
         │   ├─ For each gate in activeGates:
         │   │  └─ gate.Despawn()
         │   │     └─ poolSystem.Release(gate)
         │   └─ activeGates.Clear()
         │
         ├─→ _isGateSetActive = true
         ├─→ _choiceLocked = false
         │
         ├─→ spawnY = GetSpawnWorldY()
         │   └─ Above camera + offset
         │
         ├─→ For i = 0 to gateCount-1 (usually 3):
         │   │
         │   ├─→ config = PickGateConfig(i)
         │   │   └─ Random pick from availableGateConfigs[]
         │   │      (tries to avoid duplicates)
         │   │
         │   ├─→ laneX = GetLaneWorldX(i, gateCount)
         │   │   ├─ Split viewport into lanes
         │   │   └─ Calc world X position
         │   │
         │   ├─→ position = (laneX, spawnY, 0)
         │   │
         │   ├─→ instance = poolSystem.Spawn(gatePrefab, position)
         │   │   └─ Get from pool or instantiate
         │   │
         │   ├─→ instance.Init(config, this, playerUnit, controller, camera, pool, laneX)
         │   │   ├─ Store gate config
         │   │   ├─ Lock to X lane
         │   │   └─ doorView.Bind(config)
         │   │
         │   ├─→ instance.Spawn()
         │   │   └─ _isActive = true
         │   │
         │   └─→ activeGates.Add(instance)
         │
         └─→ _nextSpawnTime = Time.time + spawnIntervalSeconds

═════════════════════════════════════════

GATE ACTIVE LIFECYCLE
═════════════════════════════════════════
Every Frame:
    
    GateLogic.Update()
    │
    ├─ Is _isActive?     ─NO──→ return
    ├─ Is _hasLane?      ─NO──→ return
    │
    ├─ MoveStraightDown()
    │  └─ Y -= moveSpeed * Time.deltaTime
    │     (X is locked to _lockedLaneWorldX)
    │
    └─ DespawnIfOutOfBounds()
       ├─ Is below camera?
       │  └─YES─→ Despawn()
       └─ Keep moving

═════════════════════════════════════════

GATE PLAYER INTERACTION
═════════════════════════════════════════
Player touches gate trigger:

    OnTriggerStay2D(player)
    │
    └─→ GateLogic.HandlePlayerTriggered()
        │
        ├─ Is _isActive?               ─NO──→ return
        ├─ Is hitPlayer alive?          ─NO──→ return
        │
        └─ GateSystem.HandleGateChosen(this)
            │
            ├─ Is _choiceLocked?       ─YES─→ return
            │
            ├─ _choiceLocked = true
            │
            ├─→ this.ApplyEffect()
            │   │
            │   └─→ GateEffectApplier.Apply(config, mainUnit, squad)
            │       │
            │       └─ Switch config.StatTarget:
            │          │
            │          ├─ DAMAGE:
            │          │  ├─ newDamage = ApplyOp(current, op, amount)
            │          │  ├─ mainUnit.SetDamage(newDamage)
            │          │  └─ SyncFollowersDamage()
            │          │
            │          ├─ FIRE_RATE:
            │          │  ├─ newRate = ApplyOp(current, op, amount)
            │          │  ├─ mainUnit.SetFireRate(newRate)
            │          │  └─ SyncFollowersFireRate()
            │          │
            │          ├─ MAX_HP:
            │          │  └─ mainUnit.SetMaxHp(newValue)
            │          │
            │          └─ PROJECTILE_COUNT:
            │             └─ bulletSpawner.SetProjectileCount(newValue)
            │
            ├─→ Despawn other gates:
            │   └─ For each gate != this:
            │      └─ gate.Despawn()
            │         └─ poolSystem.Release()
            │
            ├─→ Despawn chosen gate if ConsumeAfterUse:
            │   └─ this.Despawn()
            │      └─ poolSystem.Release(this)
            │
            └─→ _isGateSetActive = false
                └─ Ready for next gate spawn

═════════════════════════════════════════
```

---

## 7️⃣ PLAYER DAMAGE & DEATH FLOW

```
PLAYER TAKES DAMAGE
═════════════════════════════════════════

Scenario 1: Enemy touches Player
    
    EnemyController.OnTriggerStay2D(player)
    │
    └─→ MainPlayerUnit.TakeDamage(contactDamage)
        │
        ├─ Is _isDead?      ─YES─→ return
        │
        ├─ _currentHp -= Mathf.Max(0, damage)
        ├─ RefreshHealthBar()
        │
        └─ Is _currentHp <= 0?
           │
           ├─YES─→ Die()
           └─NO──→ return

═════════════════════════════════════════

PLAYER DEATH
═════════════════════════════════════════

MainPlayerUnit.Die()
    │
    ├─ Is _isDead?      ─YES─→ return (already dead)
    │
    ├─ _isDead = true
    ├─ RefreshHealthBar() (show 0%)
    │
    └─→ Died?.Invoke(this)
        └─→ GameManager.HandlePlayerDied()
            │
            ├─→ playerController.SetControlsEnabled(false)
            │   └─ No more input
            │
            ├─→ enemySpawnerSystem.SetSpawningEnabled(false)
            │   └─ No more enemies spawn
            │
            └─→ uiSystem.ShowGameOver()
                ├─ Show final score
                ├─ Show stats
                └─ Retry button

═════════════════════════════════════════
```

---

## 8️⃣ GATE EFFECT APPLICATION: Operation Types

```
Given: baseValue (current stat)
       operationType (config)
       amount (config value)

Result: newValue

┌─────────────────────────────────────────────────────┐
│ OPERATION: ADD                                      │
├─────────────────────────────────────────────────────┤
│ newValue = baseValue + |amount|                     │
│                                                     │
│ Example:                                            │
│   Current Damage: 5.0                               │
│   Gate: +3 Damage                                   │
│   Result: 5 + 3 = 8.0                               │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ OPERATION: SUBTRACT                                │
├─────────────────────────────────────────────────────┤
│ newValue = Max(0, baseValue - |amount|)            │
│                                                     │
│ Example:                                            │
│   Current Damage: 5.0                               │
│   Gate: -3 Damage                                   │
│   Result: Max(0, 5 - 3) = 2.0                       │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ OPERATION: MULTIPLY                                │
├─────────────────────────────────────────────────────┤
│ newValue = baseValue * Max(0, |amount|)            │
│                                                     │
│ Example:                                            │
│   Current Damage: 5.0                               │
│   Gate: x2 Damage                                   │
│   Result: 5 * 2 = 10.0                              │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ OPERATION: DIVIDE                                  │
├─────────────────────────────────────────────────────┤
│ newValue = (|amount| <= 0) ? baseValue              │
│            : baseValue / |amount|                  │
│                                                     │
│ Example:                                            │
│   Current Damage: 10.0                              │
│   Gate: /2 Damage                                   │
│   Result: 10 / 2 = 5.0                              │
└─────────────────────────────────────────────────────┘
```

---

## 9️⃣ OBJECT POOL LIFECYCLE

```
POOL INITIALIZATION
═════════════════════════════════════════

PoolSystem.Awake()
    │
    └─→ For each poolDefinition:
        ├─ prefab: reference to prefab
        ├─ poolId: unique identifier
        ├─ initialSize: grow pool to this size
        │
        └─→ Create pool dictionary:
            {
                "bullet": {
                    prefab: BulletPrefab,
                    active: [],
                    inactive: [Bullet_0, Bullet_1, ...]
                },
                "enemy": {
                    prefab: EnemyPrefab,
                    active: [Enemy_0, Enemy_1, ...],
                    inactive: []
                },
                "gate": {
                    prefab: GatePrefab,
                    active: [Gate_0, Gate_1, Gate_2],
                    inactive: []
                }
            }

═════════════════════════════════════════

SPAWN FROM POOL
═════════════════════════════════════════

poolSystem.Spawn(prefab, position, rotation)
    │
    ├─→ Get pool for this prefab
    │
    ├─→ Do we have inactive object?
    │   │
    │   ├─YES:
    │   │  ├─ Get object from inactive list
    │   │  ├─ Move to active list
    │   │  ├─ SetActive(true)
    │   │  ├─ Set position, rotation
    │   │  └─ return object (REUSED)
    │   │
    │   └─NO:
    │      ├─ Instantiate(prefab, position, rotation)
    │      ├─ Add to active list
    │      └─ return object (NEW)

═════════════════════════════════════════

RELEASE TO POOL
═════════════════════════════════════════

poolSystem.Release(object)
    │
    ├─→ Get pool for this object type
    │
    ├─→ Move object from active to inactive
    │
    ├─→ SetActive(false)
    │
    ├─→ Reset state:
    │   ├─ position = (0, 0, 0)
    │   ├─ velocity = (0, 0, 0)
    │   ├─ rotation = identity
    │   └─ health/damage/etc = reset
    │
    └─ Ready for next Spawn()

═════════════════════════════════════════

POOL MEMORY BENEFIT
═════════════════════════════════════════

Without Pool (Instantiate/Destroy):
    Frame 1: Instantiate 10 bullets (10 GC allocations)
    Frame 2: Destroy 10 bullets (10 GC deallocations)
    Frame 3: Instantiate 10 bullets (10 GC allocations)
    ...
    Result: GC spikes every frame!

With Pool (Reuse):
    Init: Preallocate 20 bullets
    Frame 1: Reuse bullets 0-9 from pool
    Frame 2: Release bullets 0-9 back to pool
    Frame 3: Reuse bullets 10-19 from pool
    Result: NO GC allocation after init!

═════════════════════════════════════════
```

---

## 🔟 STATE TRANSITIONS & IMPORTANT FLAGS

```
┌─────────────────────────────────────────────────────┐
│ PLAYER STATE                                        │
├─────────────────────────────────────────────────────┤
│ _isDead: false                                      │
│ _currentHp: maxHp                                   │
│ IsInitialized: true                                 │
│                                                     │
│ ┌─ Take Damage (HP > 0)                            │
│ │  └─ _currentHp -= damage                          │
│ │     RefreshHealthBar()                            │
│ │                                                   │
│ └─ Take Final Damage (HP <= 0)                     │
│    └─ Die()                                         │
│       ├─ _isDead = true                             │
│       └─ Died?.Invoke() → GameManager              │
│          └─ Triggers Game Over                      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ BULLET STATE                                        │
├─────────────────────────────────────────────────────┤
│ _isActive: false (pooled)                           │
│                                                     │
│ ┌─ Spawn()                                          │
│ │  └─ _isActive = true                              │
│ │     _elapsedTime = 0                              │
│ │                                                   │
│ └─ Update() Loop:                                  │
│    ├─ Move bullet                                   │
│    ├─ Check collision                               │
│    ├─ Check lifetime                                │
│    └─ If over lifetime → Despawn()                │
│                                                     │
│ ┌─ Despawn()                                        │
│ │  └─ _isActive = false                             │
│ │     poolSystem.Release() → back to pool          │
│                                                     │
│ └─ OnTriggerEnter2D(enemy)                         │
│    ├─ enemy.TakeDamage()                            │
│    └─ If pierce: continue, else despawn            │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ ENEMY STATE                                         │
├─────────────────────────────────────────────────────┤
│ _isActive: false (pooled)                           │
│                                                     │
│ ┌─ Spawn()                                          │
│ │  ├─ _isActive = true                              │
│ │  ├─ currentHealth = maxHealth                     │
│ │  └─ EnsureHealthBar()                             │
│ │                                                   │
│ └─ Update() Loop:                                  │
│    ├─ Move toward player                            │
│    ├─ Check bounds                                  │
│    ├─ OnTriggerStay with player                    │
│    │  └─ player.TakeDamage()                        │
│    └─ Take damage (OnCollision with bullet)        │
│       ├─ currentHealth -= bulletDamage              │
│       └─ If currentHealth <= 0: Die()              │
│          ├─ _isDead = true                          │
│          ├─ poolSystem.Release()                    │
│          └─ Spawner.NotifyEnemyKilled()            │
│                                                     │
│ ┌─ Despawn()                                        │
│ │  └─ _isActive = false                             │
│ │     poolSystem.Release() → back to pool          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ GATE STATE                                          │
├─────────────────────────────────────────────────────┤
│ _isActive: false (pooled)                           │
│ _hasLane: will set to true on Spawn                │
│                                                     │
│ ┌─ Spawn()                                          │
│ │  ├─ _isActive = true                              │
│ │  ├─ _hasLane = true                               │
│ │  ├─ ApplyLanePosition()                           │
│ │  └─ doorView.Bind(config)                         │
│ │                                                   │
│ └─ Update() Loop:                                  │
│    ├─ MoveStraightDown()                            │
│    │  └─ Y -= moveSpeed * Time.deltaTime            │
│    └─ DespawnIfOutOfBounds()                       │
│       └─ If below camera: Despawn()                │
│                                                     │
│ ┌─ OnTriggerStay(player)                           │
│ │  └─ HandlePlayerTriggered()                       │
│ │     └─ GateSystem.HandleGateChosen()             │
│ │        ├─ ApplyEffect() on player                 │
│ │        ├─ Despawn other gates                     │
│ │        └─ _isGateSetActive = false               │
│ │                                                   │
│ └─ Despawn()                                        │
│    ├─ _isActive = false                             │
│    ├─ _hasLane = false                              │
│    └─ poolSystem.Release() → back to pool          │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ GATE SYSTEM STATE                                   │
├─────────────────────────────────────────────────────┤
│ _isGateSetActive: Are gates waiting for player?    │
│                                                     │
│ false: (default after setup)                        │
│   └─ Time.time >= _nextSpawnTime?                  │
│      └─ YES → Spawn() sets _isGateSetActive true  │
│                                                     │
│ true: (gates on screen, player can choose)         │
│   ├─ Player touches gate:                           │
│   │  └─ HandleGateChosen()                          │
│   │     ├─ Apply effect                             │
│   │     ├─ Despawn other gates                      │
│   │     └─ Sets _isGateSetActive = false           │
│   │                                                 │
│   └─ Gates despawn (no collision):                 │
│      └─ Auto set _isGateSetActive = false          │
│                                                     │
│ _choiceLocked: prevent double-choice               │
│   false: ready to accept player choice              │
│   true: player already chose, wait → reset         │
└─────────────────────────────────────────────────────┘
```

---

## 🎯 Data Flow: When Gate is Chosen

```
Player walks into Gate
           │
           ▼
GateLogic.HandlePlayerTriggered()
           │
           ▼
GateSystem.HandleGateChosen(chosenGate)
           │
           ├─→ chosenGate.ApplyEffect()
           │   │
           │   └─→ GateEffectApplier.Apply(config, mainUnit, controller)
           │       │
           │       ├─→ Switch StatTarget:
           │       │   
           │       ├─ Damage:
           │       │  │
           │       │  ├─→ newDamage = ApplyOp(mainUnit.Damage, op, amount)
           │       │  ├─→ mainUnit.SetDamage(newDamage)
           │       │  │   └─→ bulletSpawner.SetDamage(newDamage)
           │       │  └─→ SyncFollowersDamage(mainUnit.Damage)
           │       │      └─→ For each follower:
           │       │         └─ follower.SetDamage(newDamage)
           │       │
           │       ├─ FireRate:
           │       │  │
           │       │  ├─→ newRate = ApplyOp(mainUnit.FireRate, op, amount)
           │       │  ├─→ mainUnit.SetFireRate(newRate)
           │       │  │   └─→ bulletSpawner.SetFireRate(newRate)
           │       │  └─→ SyncFollowersFireRate(mainUnit.FireRate)
           │       │      └─→ For each follower:
           │       │         └─ follower.SetFireRate(newRate)
           │       │
           │       ├─ MaxHp:
           │       │  │
           │       │  ├─→ newMaxHp = ApplyOp(mainUnit.MaxHp, op, amount)
           │       │  └─→ mainUnit.SetMaxHp(newMaxHp, healByDelta)
           │       │      ├─ oldMax = maxHp
           │       │      ├─ maxHp = newMaxHp
           │       │      ├─ If healByDelta:
           │       │      │  └─ currentHp += (newMaxHp - oldMax)
           │       │      └─ RefreshHealthBar()
           │       │
           │       └─ ProjectileCount:
           │          │
           │          ├─→ current = bulletSpawner.ProjectileCount
           │          ├─→ next = Mathf.Round(ApplyOp(current, op, amount))
           │          └─→ bulletSpawner.SetProjectileCount(next)
           │
           ├─→ Despawn non-chosen gates:
           │   └─→ For each gate != chosenGate:
           │       └─ gate.Despawn()
           │          └─ poolSystem.Release()
           │
           ├─→ If ConsumeAfterUse:
           │   └─→ chosenGate.Despawn()
           │      └─ poolSystem.Release()
           │
           ├─→ _choiceLocked = false
           └─→ _isGateSetActive = false

Result: Player stats updated immediately!
        Next bullet will use new damage/fireRate/etc
        Next frame may spawn enemy at new difficulty
        Next gate spawn is 20s away
```

---

**Document Version**: v1.0  
**Created**: May 21, 2026


