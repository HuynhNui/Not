# ⚡ True Gate? - Quick Reference Card

**Cheatsheet nhanh cho nhà phát triển**

---

## 🏗️ Architecture at a Glance

```
┌─────────────────────────────────────────────────┐
│ CORE LAYER                                      │
├─────────────────────────────────────────────────┤
│ GameManager         (Orchestrator)              │
│ GameStateMachine    (State control)             │
│ PoolSystem          (Object reuse)              │
├─────────────────────────────────────────────────┤
│ GAMEPLAY LAYER                                  │
├─────────────────────────────────────────────────┤
│ PlayerController    (Squad control)             │
│ MainPlayerUnit      (Player with HP)            │
│ EnemyController     (Enemy unit)                │
│ GateLogic           (Upgrade door)              │
│ BulletSpawner       (Projectile producer)       │
├─────────────────────────────────────────────────┤
│ SYSTEMS LAYER                                   │
├─────────────────────────────────────────────────┤
│ EnemySpawnerSystem  (Enemy waves)               │
│ GateSystem          (Gate management)           │
│ CombatSystem        (Combat coordinator)        │
│ UISystem            (UI display)                │
│ LevelSystem         (Level progression)         │
│ RunStatsTracker     (Score/stats)               │
├─────────────────────────────────────────────────┤
│ DATA LAYER                                      │
├─────────────────────────────────────────────────┤
│ GateConfig          (Upgrade definition)        │
│ PlayerUnitConfig    (Player template)           │
│ EnemySpawnConfig    (Spawn rules)               │
│ EnemyArchetypeData  (Enemy templates)           │
└─────────────────────────────────────────────────┘
```

---

## 📊 Frame-by-Frame Execution

```
Each Frame (60 FPS = 16.67ms per frame):

1. PlayerMovement.Update()
   └─ Input reading + X position update

2. PlayerController.Update()
   └─ ShootSquad() → bullets spawn

3. Bullet.Update() x N
   └─ Move + collision detection

4. EnemySpawnerSystem.Update()
   └─ Check spawn timer

5. EnemyController.Update() x N
   └─ Move toward player

6. GateSystem.Update()
   └─ Check gate spawn timer

7. GateLogic.Update() x M
   └─ Move gates downscreen

8. Physics simulation (collisions)
```

---

## 🎯 Key File Locations

```
Scripts:
├─ Core/
│  ├─ GameLoop/GameManager.cs
│  └─ StateMachine/GameStateMachine.cs
├─ Gameplay/
│  ├─ Player/*
│  ├─ Combat/*
│  ├─ Enemies/EnemyController.cs
│  └─ Gates/GateLogic.cs
└─ Systems/
   ├─ EnemySpawnerSystem/
   ├─ GateSystem/
   └─ PoolSystem/

Data:
└─ Data/ScriptableObjects/
   ├─ GateConfigs/
   ├─ PlayerConfigs/
   ├─ SpawnConfigs/
   └─ EnemyArchetype/
```

---

## 🔧 Inspector Quick Setup

### MainPlayer Rigidbody2D (CRITICAL!)
```
✓ Body Type: Dynamic
✓ Gravity Scale: 0
✓ Constraints: 2 (Freeze Y)
✓ Velocity: 0, 0, 0
```

### GateSystem Key Fields
```
✓ gatePrefab: Set to gate prefab
✓ spawnIntervalSeconds: 20
✓ gateCount: 3
✓ availableGateConfigs: Array of gates
```

### EnemySpawnerSystem Key Fields
```
✓ enemyPrefab: Set to enemy prefab
✓ baseSpawnInterval: 1.5
✓ minimumSpawnInterval: 0.35
✓ difficultyCurve: Configured
```

### PlayerController Key Fields
```
✓ mainPlayerUnit: Drag MainPlayer here
✓ autoFire: true
✓ playerMovement: (auto)
```

---

## 💻 Common Code Snippets

### Damage Player
```csharp
mainPlayerUnit.TakeDamage(10f);
```

### Apply Gate Effect
```csharp
GateEffectApplier.Apply(gateConfig, mainUnit, squad);
```

### Spawn Enemy
```csharp
enemySpawnerSystem.Spawn();
```

### Set Player Stats
```csharp
mainPlayerUnit.SetDamage(15f);
mainPlayerUnit.SetFireRate(6f);
mainPlayerUnit.SetMaxHp(20f, healByDelta: true);
bulletSpawner.SetProjectileCount(3);
```

### Disable Spawning (on pause/gameover)
```csharp
playerController.SetControlsEnabled(false);
enemySpawnerSystem.SetSpawningEnabled(false);
```

---

## 🐛 Quick Debug Commands

### Check Pool Status
```csharp
// In PoolSystem.cs:
public void PrintStatus()
{
    foreach (var pool in _pools)
        Debug.Log($"{pool.Key}: Active={pool.Value.Count}");
}
```

### Log Difficulty
```csharp
// In EnemySpawnerSystem.Update():
float mult = difficultyCurve.Evaluate(_elapsedTime);
Debug.Log($"Difficulty: {mult:F2}x at {_elapsedTime:F1}s");
```

### Check Gate Spawn
```csharp
// In GateSystem.Update():
Debug.Log($"Next gate spawn: {_nextSpawnTime - Time.time:F1}s");
```

---

## ⚠️ Common Mistakes

```
❌ Forget to assign mainPlayerUnit reference
   └─ Result: PlayerController.ShootSquad() does nothing

❌ Set GravityScale = 0 but not Freeze Y constraints
   └─ Result: Player falls off screen

❌ Empty availableGateConfigs array
   └─ Result: No gates spawn

❌ Set baseSpawnInterval = 0
   └─ Result: Infinite enemy spawning, game crashes

❌ Put Instantiate() in tight loops
   └─ Result: GC spikes, frame drops

❌ Forget to add poolDefinitions to PoolSystem
   └─ Result: Fallback to Instantiate() instead of pool

❌ Leave debugging Profiler running in builds
   └─ Result: Poor performance, drain battery
```

---

## 📈 Difficulty Scaling Formula

```
Current Difficulty Multiplier = AnimationCurve.Evaluate(elapsedTime)

Example curve:
  t=0s    → mult=1.0x  → spawn interval = 1.5s
  t=60s   → mult=1.75x → spawn interval = 0.86s
  t=120s  → mult=2.5x  → spawn interval = 0.6s

Formula: scaledInterval = baseInterval / multiplier
         finalInterval = Max(minimumInterval, scaledInterval)
```

---

## 🎮 Gameplay Feel Tweaks

### Make Game Easier
```
Decrease difficulty curve slope (gentler ramp)
Increase baseSpawnInterval (spawn slower)
Increase player damage
Decrease player fire rate cooldown
Increase gate spawn interval (more time to breathe)
Increase player max HP
```

### Make Game Harder
```
Increase difficulty curve (steeper ramp)
Decrease baseSpawnInterval (spawn faster)
Decrease minimumSpawnInterval (faster cap)
Decrease player damage
Increase enemy damage
Decrease player max HP
Decrease gate spawn interval (less time to breathe)
```

---

## 📱 Mobile Optimization Checklist

- [ ] Player can move with touch
- [ ] Buttons are >44x44pt (touch size)
- [ ] < 100 draw calls
- [ ] < 500MB memory usage
- [ ] 60 FPS on mid-range device
- [ ] No GC allocations during gameplay
- [ ] Sprite atlases used (not individual textures)
- [ ] No 3D models (2D only)
- [ ] Audio: MP3 or OGG (compressed)
- [ ] Profiled on real device

---

## 🔄 Update Order (Conceptual)

```
1. Input Reading (PlayerMovement.Update)
2. State Updates (PlayerController, EnemySpawner, GateSystem)
3. Movement Updates (Bullet, Enemy, Gate)
4. Physics Simulation (collisions, triggers)
5. Rendering (Sprites, UI)

Important: Don't modify objects being iterated!
Bad:  foreach (enemy) { if (dead) enemies.Remove(enemy); }
Good: for (int i = list.Count - 1; i >= 0; i--) { if (dead) list.RemoveAt(i); }
```

---

## 🎯 Performance Targets

```
FPS:                60 (mobile), 120+ (desktop)
Frame Time:         16.67ms (60 FPS)
GC Alloc/frame:     0 B (zero after init)
Memory:             < 500MB
Active Enemies:     50-100
Active Bullets:     50+
Active Gates:       3-6
Draw Calls:         < 100
Sprite Count:       < 200
```

---

## 📝 Code Structure Template

### New System Template
```csharp
public sealed class NewSystem : MonoBehaviour
{
    [SerializeField] private SomeComponent reference;
    
    private bool _isActive;
    private float _timer;
    
    private void Awake() { Init(); }
    
    public void Init()
    {
        _isActive = true;
        _timer = 0f;
    }
    
    private void Update()
    {
        if (!_isActive) return;
        
        _timer += Time.deltaTime;
        // Logic here
    }
    
    public void SetActive(bool active) { _isActive = active; }
}
```

### New GameplayUnit Template
```csharp
public class NewUnit : MonoBehaviour
{
    protected bool _isActive;
    protected float _lifetime;
    
    public virtual void Init() { }
    public virtual void Spawn() { _isActive = true; }
    public virtual void Despawn() { _isActive = false; }
    
    protected virtual void Update()
    {
        if (!_isActive) return;
        _lifetime += Time.deltaTime;
    }
}
```

---

## 🚀 Testing Shortcuts

```
To test enemy spawning quickly:
  1. Play scene
  2. Wait 2 seconds
  3. Check if enemy appears above camera
  4. Watch it move toward player

To test gate system:
  1. Play scene
  2. Wait 20 seconds
  3. Check if 3 gates appear
  4. Walk through one
  5. Check if stat changed

To test player damage:
  1. Play scene
  2. Let enemy reach player
  3. Check health bar decreases
  4. Watch until HP = 0 → Game Over

To test pool:
  1. Open Profiler (Memory tab)
  2. Play for 30 seconds
  3. Check GC.Alloc = 0 B
```

---

## 📖 Related Documents

- **CODE_FLOW_GUIDE.md** - Detailed flow explanation
- **CODE_FLOW_DIAGRAMS.md** - ASCII flow diagrams
- **SETUP_AND_DEVELOPMENT_GUIDE.md** - Setup instructions

---

## 🎓 Learning Path

1. **Read**: CODE_FLOW_GUIDE.md (understand structure)
2. **Study**: CODE_FLOW_DIAGRAMS.md (trace execution)
3. **Setup**: SETUP_AND_DEVELOPMENT_GUIDE.md (build scene)
4. **Reference**: This file (quick lookups)
5. **Code**: Modify and experiment!

---

## 💡 Pro Tips

✨ Use Ctrl+F to search in this document
✨ Keep Profiler open while developing
✨ Test on target device before polish
✨ Comment your assumptions
✨ Name variables clearly
✨ Avoid premature optimization
✨ Profile before optimizing
✨ Use object pooling early
✨ Cache expensive lookups
✨ Version your ScriptableObjects

---

**Quick Reference Version**: v1.0  
**Last Updated**: May 21, 2026  
**For**: True Gate? - Mobile Survival Shooter


