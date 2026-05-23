# 🛠️ True Gate? - Setup & Development Guide

**Hướng dẫn thiết lập và best practices cho nhà phát triển**

---

## 📋 Table of Contents

1. [Project Setup](#project-setup)
2. [Object Pooling Setup](#object-pooling-setup)
3. [Data Configuration](#data-configuration)
4. [Common Tasks](#common-tasks)
5. [Debugging Tips](#debugging-tips)
6. [Performance Optimization](#performance-optimization)
7. [Known Issues & Solutions](#known-issues--solutions)

---

## Project Setup

### Initial Scene Construction

**Step 1: Create GameManager**
```
Create Empty GameObject
  ├─ Add: GameManager.cs
  ├─ Add: GameStateMachine.cs
  ├─ Add: References:
  │  ├─ gameStateMachine (self)
  │  ├─ combatSystem
  │  ├─ enemySpawnerSystem
  │  ├─ gateSystem
  │  ├─ uiSystem
  │  ├─ levelSystem
  │  ├─ playerController
  │  └─ mainPlayerUnit
  └─ Tag: "GameManager"
```

**Step 2: Create PoolSystem**
```
Create Empty GameObject
  ├─ Add: PoolSystem.cs
  ├─ Configure poolDefinitions[]:
  │  ├─ poolId: "bullet"
  │  │  └─ prefab: Your bullet prefab
  │  ├─ poolId: "enemy"
  │  │  └─ prefab: Your enemy prefab
  │  └─ poolId: "gate"
  │     └─ prefab: Your gate prefab
  └─ Name: "PoolSystem"
```

**Step 3: Create Player**
```
Create GameObject: "PlayerRoot"
  ├─ Add: Transform
  ├─ Position: (0, -4, 0)
  │
  └─ Child: "MainPlayer"
     ├─ Add: MainPlayerUnit.cs
     ├─ Add: PlayerUnit.cs (base)
     ├─ Add: BulletSpawner.cs
     ├─ Add: SpriteRenderer
     ├─ Add: BoxCollider2D
     ├─ Add: Rigidbody2D (IMPORTANT SETTINGS!)
     │  ├─ Body Type: Dynamic
     │  ├─ Gravity Scale: 0
     │  ├─ Constraints: 2 (Freeze Y)
     │  └─ Collision Detection: Continuous
     │
     ├─ Properties:
     │  ├─ mainUnitConfig (ScriptableObject)
     │  ├─ maxHp: 10
     │  ├─ bulletSpawner (self)
     │  └─ damage: 1, fireRate: 4
     │
     └─ Child: "FirePoint"
        └─ Position offset where bullets spawn
        
Parent "PlayerRoot" Add:
  ├─ PlayerController.cs
  │  ├─ playerMovement (child script)
  │  ├─ mainPlayerUnit (MainPlayer reference)
  │  └─ autoFire: true
  │
  └─ PlayerMovement.cs
     ├─ moveSpeed: 5
     ├─ gameplayCamera: Main Camera ref
     ├─ horizontalClamp: 3.5
     ├─ useCameraBounds: true
     └─ (Add Rigidbody2D for movement constraints)
```

**Step 4: Create Camera Setup**
```
Main Camera
  ├─ Camera
  │  ├─ orthographic: true
  │  ├─ orthographicSize: 5
  │  └─ Position: (0, 0, -10)
  ├─ Tag: "MainCamera"
  └─ AudioListener
```

**Step 5: Create Enemy System**
```
Create Empty GameObject: "EnemySpawnerSystem"
  ├─ Add: EnemySpawnerSystem.cs
  ├─ Properties:
  │  ├─ enemyPrefab: Enemy prefab
  │  ├─ spawnConfig: (ScriptableObject, optional)
  │  ├─ difficultyCurve: AnimationCurve
  │  │  └─ Key at t=0 value=1, t=120 value=2.5
  │  ├─ baseSpawnInterval: 1.5
  │  ├─ minimumSpawnInterval: 0.35
  │  ├─ playerUnit: (auto-find)
  │  ├─ gameplayCamera: (auto-find or set Main Camera)
  │  ├─ poolSystem: (auto-find)
  │  └─ spawnPoints: (optional Transform array)
  │
  └─ Child: "SpawnPoint" (optional)
     └─ Position above camera where enemies spawn
```

**Step 6: Create Gate System**
```
Create Empty GameObject: "GateSystem"
  ├─ Add: GateSystem.cs
  ├─ Properties:
  │  ├─ Spawning:
  │  │  ├─ gatePrefab: Your gate prefab
  │  │  ├─ spawnIntervalSeconds: 20
  │  │  ├─ spawnAboveCameraOffset: 1.25
  │  │  ├─ useViewportLanes: true
  │  │  ├─ viewportLaneMin: 0.12
  │  │  ├─ viewportLaneMax: 0.88
  │  │  ├─ gateCount: 3
  │  │  └─ laneSpacing: 2.2
  │  │
  │  ├─ Configs:
  │  │  └─ availableGateConfigs: Array of GateConfig SO
  │  │
  │  └─ Runtime References:
  │     ├─ playerController: (auto-find)
  │     ├─ mainPlayerUnit: (auto-find)
  │     ├─ gameplayCamera: (auto-find)
  │     └─ poolSystem: (auto-find)
  │
  └─ (No children needed)
```

---

## Object Pooling Setup

### PoolSystem Configuration

#### Why Object Pooling?
```
Without Pooling:
  ✗ Instantiate() creates GC allocation
  ✗ Destroy() creates GC deallocation
  ✗ Causes GC spikes (frame drops)
  ✗ Especially bad for mobile

With Pooling:
  ✓ Pre-allocate objects at startup
  ✓ Reuse instead of create/destroy
  ✓ Zero GC spikes in gameplay
  ✓ Predictable memory usage
  ✓ Better performance on mobile
```

#### Pool Definition

```csharp
[System.Serializable]
public class PoolDefinition
{
    public string poolId;           // "bullet", "enemy", "gate"
    public T prefab;                // Reference to prefab
    public int initialSize;         // How many to pre-allocate
}
```

#### Setup Example

```json
PoolSystem poolDefinitions:
[0]
  poolId: "bullet"
  prefab: Assets/Prefabs/Bullet.prefab
  initialSize: 100      // Pre-allocate 100 bullets
  
[1]
  poolId: "enemy"
  prefab: Assets/Prefabs/Enemy.prefab
  initialSize: 50       // Pre-allocate 50 enemies
  
[2]
  poolId: "gate"
  prefab: Assets/Prefabs/Gate.prefab
  initialSize: 10       // Pre-allocate 10 gates
```

#### Pool Size Calculator

```
Bullet Pool:
  Max bullets on screen: projectileCount * fireRate * max_lifetime
  Typical: 1 * 4 shots/sec * 5 sec lifetime = 20 bullets
  Safe size: 100+ (allows burst)

Enemy Pool:
  Max enemies: depends on enemy lifetime + spawn rate
  Difficulty: spawn every 0.35s at high difficulty
  Lifetime: ~10-15 seconds to reach player
  Calculation: (15 sec / 0.35 spawn rate) = ~43 enemies
  Safe size: 50-100

Gate Pool:
  Gates spawn in groups of 3 every 20 seconds
  Lifetime: ~30 seconds (if not triggered)
  Gates per run: 3 gates * (120 sec run / 20 sec spawn) = 18 gates
  Safe size: 20
```

#### Memory Impact

```
Bullet (approx 200 bytes each):
  100 bullets × 200 bytes = 20 KB

Enemy (approx 500 bytes each):
  50 enemies × 500 bytes = 25 KB

Gate (approx 300 bytes each):
  10 gates × 300 bytes = 3 KB

Total: ~50 KB overhead (acceptable)
Benefit: Zero GC allocation during gameplay!
```

---

## Data Configuration

### Creating ScriptableObjects

#### 1. GateConfig

```csharp
// File: Assets/Resources/GateConfigs/DamageAdd10.asset

[CreateAssetMenu(menuName = "True Gate/Gate Config")]
public class GateConfig : ScriptableObject
{
    public string gateId = "damage_add_10";
    public string displayName = "+10 Damage";
    public GateStatTarget statTarget = GateStatTarget.Damage;
    public GateOperationType operationType = GateOperationType.Add;
    public float amount = 10f;
    public string description = "Increase damage by 10";
}
```

**Step-by-step creation:**
```
1. Right-click in Assets/Resources/GateConfigs/
2. Create > True Gate > Gate Config
3. Name: DamageAdd10
4. Set in Inspector:
   ├─ gateId: "damage_add_10"
   ├─ displayName: "+10 Damage"
   ├─ statTarget: Damage
   ├─ operationType: Add
   ├─ amount: 10
   └─ description: "Increase damage by 10"
5. Save
```

**Enum Values:**

```csharp
public enum GateStatTarget
{
    Damage,           // Affects weapon damage
    FireRate,         // Affects bullets per second
    MaxHp,            // Affects max health
    ProjectileCount   // Affects bullets per shot
}

public enum GateOperationType
{
    Add,              // value = base + amount
    Subtract,         // value = max(0, base - amount)
    Multiply,         // value = base * amount
    Divide            // value = base / amount
}
```

#### 2. EnemySpawnConfig

```csharp
// File: Assets/Resources/SpawnConfigs/NormalDifficulty.asset

[CreateAssetMenu(menuName = "True Gate/Enemy Spawn Config")]
public class EnemySpawnConfig : ScriptableObject
{
    public AnimationCurve difficultyCurve;
    public float baseSpawnInterval = 1.5f;
    public float minimumSpawnInterval = 0.35f;
    public float spawnYOffset = 1f;
    public float horizontalSpawnPadding = 0.35f;
}
```

**Setup Animation Curve:**
```
Create new AnimationCurve in Inspector:
├─ Key 1:
│  ├─ time: 0
│  ├─ value: 1.0
│  └─ type: Linear
│
└─ Key 2:
   ├─ time: 120
   ├─ value: 2.5
   └─ type: Linear

This creates a linear difficulty scale:
  0s   → 1.0x difficulty (spawn every 1.5s)
  60s  → 1.75x (spawn every 0.86s)
  120s → 2.5x difficulty (spawn every 0.6s)
```

#### 3. PlayerUnitConfig

```csharp
// File: Assets/Resources/PlayerConfigs/DefaultPlayer.asset

[CreateAssetMenu(menuName = "True Gate/Player Unit Config")]
public class PlayerUnitConfig : ScriptableObject
{
    public float damage = 1f;
    public float fireRate = 4f;
    public float maxHealth = 10f;
    public float bulletSpeed = 12f;
    public int projectileCount = 1;
}
```

---

## Common Tasks

### Task 1: Add New Gate Type

```
1. Create GateConfig ScriptableObject
   Right-click > Create > True Gate > Gate Config
   Name: FireRateMultiply1_5
   
2. Configure in Inspector:
   ├─ gateId: "fire_rate_multiply_1_5"
   ├─ displayName: "x1.5 Fire Rate"
   ├─ statTarget: FireRate
   ├─ operationType: Multiply
   ├─ amount: 1.5
   └─ description: "Increase fire rate by 50%"
   
3. Add to GateSystem.availableGateConfigs[]
   ├─ Select GateSystem GameObject
   ├─ Inspector > availableGateConfigs
   ├─ Size + 1
   ├─ Drag new config into slot
   
4. Done! Gate can now be selected randomly
```

### Task 2: Add New Enemy Type

```
1. Create Enemy Prefab
   ├─ Duplicate existing enemy prefab
   ├─ Modify:
   │  ├─ Sprite
   │  ├─ Health: 20 (more tank)
   │  ├─ MoveSpeed: 2 (slower)
   │  └─ ContactDamage: 2 (more damage)
   └─ Save as: Assets/Prefabs/TankEnemy.prefab
   
2. Add to PoolSystem poolDefinitions[]
   ├─ Select PoolSystem
   ├─ poolDefinitions size + 1
   ├─ New entry:
   │  ├─ poolId: "tank_enemy"
   │  ├─ prefab: TankEnemy prefab
   │  └─ initialSize: 20
   
3. (Optional) Create data-driven enemy selection
   ├─ This would require EnemyArchetypeData system
   ├─ For now: update spawn logic manually
   
4. Done!
```

### Task 3: Tune Difficulty Curve

```
Current setup is too easy? Difficulty not ramping?

1. Select GateSystem
2. Inspector > spawnIntervalSeconds
   Decrease to spawn gates faster: 20 → 15
   
3. Select EnemySpawnerSystem
4. Tweaks:
   ├─ baseSpawnInterval: 1.5 → 1.0 (spawn faster)
   ├─ minimumSpawnInterval: 0.35 → 0.25 (lower floor)
   ├─ difficultyCurve: Edit curve shape
   │  ├─ Add key at t=60, value=3.0 (steeper)
   │  └─ Adjust handles for curve shape
   
5. Test! (Play 2 minutes and see enemy spawn rate)
   ├─ At 0s: should see enemies every 1-2s
   ├─ At 60s: should see enemies every 0.5-0.7s
   └─ At 120s: should be chaos!
```

### Task 4: Adjust Player Stats

```
1. Select MainPlayer GameObject
2. Inspector:
   ├─ MainPlayerUnit:
   │  ├─ maxHp: 10 → 20 (more health)
   │  └─ damage: 1 → 2 (more damage)
   ├─ PlayerUnit (base):
   │  └─ fireRate: 4 → 6 (shoot faster)
   ├─ BulletSpawner:
   │  ├─ bulletSpeed: 12 → 15 (bullets faster)
   │  ├─ projectileCount: 1 → 3 (multi-shot)
   │  └─ fireRate: 4 (sync with PlayerUnit)
   
3. Test immediately in Play mode
4. Adjust as needed
```

---

## Debugging Tips

### Enable Debug Logs

```csharp
// Add to any system you want to debug:

private void Update()
{
    Debug.Log($"[EnemySpawner] Elapsed: {_elapsedTime:F1}s, Next spawn in: {GetCurrentSpawnInterval():F2}s");
}

private void Spawn()
{
    Debug.Log($"[EnemySpawner] Spawning enemy at position {spawnPosition}");
}
```

### Monitor Pools at Runtime

```csharp
// In PoolSystem, add this for debugging:

public void DebugPrintStatus()
{
    foreach (var pool in _pools)
    {
        Debug.Log($"Pool '{pool.Key}': Active={pool.Value.active.Count}, Inactive={pool.Value.inactive.Count}");
    }
}

// Call from GameManager or Inspector button
```

### Check Collision Detection

```
Common issue: Player not taking damage from enemies

Solution 1: Check Colliders
  ├─ Player: BoxCollider2D
  │  └─ Is Trigger: NO (should be solid)
  ├─ Enemy: BoxCollider2D
  │  └─ Is Trigger: YES (sensor only)
  └─ Rigidbody2D on enemy: Body Type Dynamic

Solution 2: Check OnTriggerStay2D
  └─ Attach GateTrigger.cs to Gate prefab
     └─ Must have Is Trigger: YES

Solution 3: Check Physics settings
  └─ Project Settings > Physics2D
     ├─ Gravity: (0, 0)
     └─ Default Material friction: 0
```

### Frame Rate Issues

```
Game running slow? Check:

1. Profiler (Window > Analysis > Profiler)
   ├─ CPU Usage tab
   ├─ Look for spikes in:
   │  ├─ Physics.ProcessColliders
   │  ├─ Rendering
   │  └─ Scripts
   └─ Memory tab: watch GC Alloc

2. Common culprits:
   ├─ Too many active enemies (100+)
   ├─ Physics checks too expensive
   ├─ GC allocation in Update()
   ├─ Too many visual effects

3. Optimizations:
   ├─ Increase enemy pool size
   ├─ Reduce minimum spawn interval
   ├─ Disable collider checks for off-screen objects
   ├─ Use object pooling (already done!)
   └─ Profile before optimize!
```

---

## Performance Optimization

### Memory Profiling Checklist

```
✓ Launch the game in Editor
✓ Open Profiler (Window > Analysis > Profiler)
✓ Switch to "Memory" tab
✓ Play for 2 minutes
✓ Check for:
  ├─ Smooth memory line (no spikes)
  ├─ GC.Alloc is "0 B" during gameplay
  └─ Total memory < 50 MB for mobile
```

### GC Alloc in Physics

```
Avoid these in Update():

  ❌ sphereCast = Physics.OverlapSphere(position, radius)
  ❌ raycast = Physics.Raycast(origin, direction)
  ❌ colliders = Physics.OverlapBox(center, size)

Prefer these:

  ✓ OnTriggerEnter2D() (cached by engine)
  ✓ OnTriggerStay2D() (instant check)
  ✓ OnTriggerExit2D() (instant check)
```

### Common Bottlenecks

```
1. Too many enemies active
   └─ Solution: Increase spawn cap or despawn faster

2. Too many projectiles
   └─ Solution: Limit projectile count or pierce better

3. Physics checks every frame
   └─ Solution: Use OnTrigger callbacks instead

4. FindAnyObjectByType() called every frame
   └─ Solution: Cache references in Init() or Awake()

5. String operations (Debug.Log)
   └─ Solution: Disable debug logs in builds
```

### Mobile Optimization

```
Target: 60 FPS on mid-range Android/iOS

Targets:
  ├─ CPU: < 16.67ms per frame (60 FPS)
  ├─ Memory: < 500 MB
  ├─ Battery: < 10% drain per hour
  └─ Draw Calls: < 100

Optimizations:
  ├─ Use sprite atlases (reduce draw calls)
  ├─ Limit particle effects
  ├─ Reduce audio quality
  ├─ Disable post-processing
  ├─ Use object pooling (✓ done)
  └─ Profile on real device!
```

---

## Known Issues & Solutions

### Issue #1: Player Falls Down After Spawn

**Problem:**
```
Player spawns but immediately starts falling
```

**Cause:**
```
Rigidbody2D settings missing:
  ├─ GravityScale = 0 ✓ set
  ├─ Constraints = 2 (Freeze Y) ✗ NOT SET!
```

**Solution:**
```
1. Select MainPlayer GameObject
2. Inspector > Rigidbody2D
3. Set m_Constraints: 2 (Freeze Y Position)
4. Done! Player should stay in place.
```

### Issue #2: Player Doesn't Shoot

**Problem:**
```
Player should be shooting but no bullets appear
```

**Causes & Solutions:**

```
✗ PlayerController.mainPlayerUnit is NULL
  Solution: Drag MainPlayer into playerController.mainPlayerUnit field
  
✗ BulletSpawner.bulletPrefab is NULL
  Solution: Drag bullet prefab into bulletSpawner.bulletPrefab field
  
✗ fireRate is 0
  Solution: Set fireRate > 0 (default: 4)
  
✗ PoolSystem not found
  Solution: Create PoolSystem GameObject first
            OR assign poolSystem field manually
            
✗ Bullet prefab missing IPoolable interface
  Solution: Add IPoolable to Bullet.cs
```

### Issue #3: No Enemies Spawn

**Problem:**
```
EnemySpawnerSystem running but no enemies appear
```

**Causes & Solutions:**

```
✗ enemyPrefab is NULL
  Solution: Assign enemy prefab to enemySpawnerSystem.enemyPrefab
  
✗ _spawningEnabled is false
  Solution: Check if SetSpawningEnabled(false) was called
  
✗ playerUnit is NULL or dead
  Solution: Ensure MainPlayerUnit exists and is not dead
  
✗ PoolSystem not configured correctly
  Solution: Verify PoolSystem has enemy in poolDefinitions[]
  
✗ Spawn interval very high
  Solution: Check baseSpawnInterval (default: 1.5s is good)
```

### Issue #4: Gates Don't Move

**Problem:**
```
Gates spawn but don't move down screen
```

**Causes & Solutions:**

```
✗ GateLogic._isActive is false
  Solution: Call GateLogic.Spawn() after initialization
  
✗ moveSpeed is 0
  Solution: Set moveSpeed > 0 (default: 3)
  
✗ Rigidbody is locked
  Solution: Check m_Constraints on gate Rigidbody2D
            Remove "Freeze Y" constraint
  
✗ GateSystem._isGateSetActive stays true
  Solution: Ensure HandleGateChosen() is called when player touches gate
```

### Issue #5: Massive Frame Drops

**Problem:**
```
Game runs 60 FPS but suddenly drops to 10 FPS
```

**Cause:**
```
Object pooling not set up → Instantiate/Destroy happening
  GC allocation → GC pause → Frame drop!
```

**Solution:**

```
1. Open Profiler > Memory tab
2. Look for GC.Alloc spikes aligned with frame drops
3. If confirmed:
   ├─ Check PoolSystem setup
   ├─ Verify bullet/enemy/gate use pooling
   └─ Profile which object is causing allocation
4. Alternative: Increase pool sizes
```

### Issue #6: Gate Effect Not Applied

**Problem:**
```
Player walks through gate but nothing changes
```

**Causes & Solutions:**

```
✗ availableGateConfigs is empty
  Solution: Add GateConfigs to GateSystem.availableGateConfigs[]
  
✗ Gate not triggering
  Solution: Verify GateTrigger has Is Trigger: YES
  
✗ Player not detecting collision
  Solution: Verify Player has BoxCollider2D (not trigger)
  
✗ GateEffectApplier has no case for StatTarget
  Solution: Check GateEffectApplier.Apply() switch statement
            Add missing StatTarget case if needed
  
✗ Effect applied but invisible
  Solution: Check if BulletSpawner reflects new stats
            Call bulletSpawner.Initialize() after stat change
```

---

## Useful Editor Tools

### Quick Test Setup

```csharp
#if UNITY_EDITOR
using UnityEditor;

public class GameSetupTools
{
    [MenuItem("Tools/True Gate/Setup Scene")]
    public static void SetupScene()
    {
        // Auto-create and configure scene
        // Saves time during development
    }
    
    [MenuItem("Tools/True Gate/Spawn 100 Enemies")]
    public static void Spawn100Enemies()
    {
        var spawner = FindObjectOfType<EnemySpawnerSystem>();
        for (int i = 0; i < 100; i++)
        {
            spawner.Spawn();
        }
    }
}
#endif
```

### Debug Visualization

```csharp
private void OnDrawGizmosSelected()
{
    // Draw spawn bounds
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireCube(spawnCenter, spawnSize);
    
    // Draw player bounds
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(transform.position, playerSize);
}
```

---

## Development Checklist

### Before First Playtest

- [ ] PlayerController has mainPlayerUnit reference
- [ ] PoolSystem is set up with bullet/enemy/gate definitions
- [ ] Player prefab has Rigidbody2D with Freeze Y
- [ ] Camera is set to orthographic
- [ ] At least one GateConfig in availableGateConfigs
- [ ] EnemySpawnerSystem has enemy prefab assigned
- [ ] GateSystem has gate prefab assigned
- [ ] All colliders are set up correctly
- [ ] Game runs at > 30 FPS

### Before Submission

- [ ] No infinite loops
- [ ] No memory leaks (check Profiler)
- [ ] No GC spikes during gameplay
- [ ] Player can't fall through screen
- [ ] Game Over when player dies
- [ ] Difficulty ramps up over time
- [ ] Gates spawn and apply effects correctly
- [ ] Enemies scale with difficulty
- [ ] Performance target: 60 FPS on target device

---

**Document Version**: v1.0  
**Created**: May 21, 2026


