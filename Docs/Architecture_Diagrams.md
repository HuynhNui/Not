# True Gate Architecture Diagrams

Tai lieu nay gom 2 diagram chinh co the dan truc tiep vao bao cao:

- Class diagram: mo ta cac class quan trong va quan he giua chung.
- Layered architecture: mo ta kien truc he thong theo cac tang.

## Class Diagram

```mermaid
classDiagram
    direction LR

    class GameManager {
        +Init()
        -StartRun()
        -PauseRun()
        -ResumeRun()
        -HandleSquadDefeated()
        -ShowGameOverScreen()
    }

    class GameStateMachine {
        +GameState CurrentState
        +SetState(GameState nextState)
        +event StateChanged
    }

    class GameState {
        <<enumeration>>
        Bootstrap
        MainMenu
        Playing
        Cutscene
        Paused
        GameOver
    }

    class UISystem {
        +Init(RunStatsTracker tracker)
        +ShowMainMenu()
        +ShowGameplayHud()
        +ShowPause()
        +ShowGameOver()
        +event PlayRequested
    }

    class RunStatsTracker {
        +BeginRun()
        +EndRun()
        +CreateSnapshot()
    }

    class PlayerController {
        +MainPlayerUnit MainPlayerUnit
        +IReadOnlyList~FollowerUnit~ Followers
        +SetControlsEnabled(bool enabled)
        +SetSquadCount(int targetCount)
        +ShootSquad()
        +event SquadDefeated
    }

    class PlayerUnit {
        +float Damage
        +float FireRate
        +float CurrentHp
        +float MaxHp
        +bool IsDead
        +Initialize()
        +Shoot()
        +TakeDamage(float value)
    }

    class MainPlayerUnit
    class FollowerUnit

    class PlayerMovement {
        +Init()
    }

    class EnemySpawnerSystem {
        +BeginRun()
        +SetSpawningEnabled(bool enabled)
        +Spawn()
        +event EnemyKilled
    }

    class EnemyController {
        +int ScoreValue
        +int CoinReward
        +bool IsActive
        +Init()
        +Spawn()
        +Despawn()
        +TakeDamage(float damageAmount)
    }

    class GateSystem {
        +BeginRun()
        +SetSpawningEnabled(bool enabled)
        +Spawn()
        +event GateShown
        +event GateSelected
    }

    class GateLogic {
        +GateConfig Config
        +Init(GateConfig config)
        +Apply()
    }

    class GateRuntimeEffectController {
        +Configure()
        +BeginRun()
        +ApplyTimedEffect()
    }

    class GateEffectApplier {
        <<static>>
        +Apply()
    }

    class BulletSpawner {
        +Shoot()
        +SetDamage(float value)
        +SetFireRate(float value)
        +SetProjectileCount(int value)
    }

    class Bullet {
        +Init()
        +Launch()
        +OnSpawned()
        +OnDespawned()
    }

    class IBulletModifier {
        <<interface>>
        +Tick()
        +OnHit()
    }

    class HomingModifier
    class PierceModifier
    class SplitModifier

    class IDamageable {
        <<interface>>
        +TakeDamage(float value)
    }

    class IConditionalDamageable {
        <<interface>>
        +CanReceiveDamageFrom(Object source)
    }

    class IPoolable {
        <<interface>>
        +OnSpawned()
        +OnDespawned()
    }

    class PoolSystem {
        +Get()
        +Release()
    }

    class SaveService {
        <<singleton>>
        +SaveData Data
        +EnsureLoaded()
        +LoadAsync()
        +SaveAsync()
        +RecordRunResult()
        +RecordCutsceneSeen()
    }

    class SaveData {
        +int totalRunsCompleted
        +int totalEnemyKills
        +int walletCoins
        +bool HasSeenCutscene(string id)
        +bool MarkCutsceneSeen(string id)
    }

    class StoryCutsceneRuntimeController {
        +bool IsPlaying
        +Init()
        +TryPlayInitialCutscene()
        +TryPlayPostRunCutscene()
        +TryPlayCutscene(string id)
    }

    class StoryCutsceneDirector {
        +Play(string cutsceneId)
        +OnCutsceneStarted
        +OnCutsceneFinished
    }

    class StoryCutsceneUnlockRules {
        <<static>>
        +IsEligible()
        +TryGetFirstEligible()
        +NormalizePlayableCutsceneId()
    }

    class BalanceBootstrapConfig {
        <<ScriptableObject>>
        +CombatScalingConfig CombatScalingConfig
        +GatePoolConfig GatePoolConfig
        +RunPressureConfig RunPressureConfig
        +EconomyConfig EconomyConfig
    }

    class GateConfig {
        <<ScriptableObject>>
        +GateStatTarget Target
        +GateOperationType Operation
        +float Value
    }

    class GatePoolConfig {
        <<ScriptableObject>>
        +float GateCadenceSeconds
        +float MajorGateCadenceSeconds
    }

    class PlayerUnitConfig {
        <<ScriptableObject>>
        +float MaxHealth
        +float Damage
        +float FireRate
        +float BulletSpeed
    }

    class EnemySpawnConfig {
        <<ScriptableObject>>
    }

    class RunPressureConfig {
        <<ScriptableObject>>
    }

    GameManager --> GameStateMachine : controls state
    GameStateMachine --> GameState : uses
    GameManager --> UISystem : updates screens
    GameManager --> PlayerController : starts and stops player
    GameManager --> EnemySpawnerSystem : starts and stops spawning
    GameManager --> GateSystem : starts and stops gates
    GameManager --> RunStatsTracker : records run
    GameManager --> SaveService : loads and saves progress
    GameManager --> StoryCutsceneRuntimeController : plays story moments
    GameManager --> BalanceBootstrapConfig : reads balance setup

    PlayerController *-- MainPlayerUnit : owns main unit
    PlayerController o-- FollowerUnit : manages squad
    PlayerController --> PlayerMovement : reads movement
    MainPlayerUnit --|> PlayerUnit
    FollowerUnit --|> PlayerUnit
    PlayerUnit ..|> IDamageable
    PlayerUnit --> BulletSpawner : fires through
    PlayerUnit --> PlayerUnitConfig : reads stats

    BulletSpawner --> Bullet : spawns
    Bullet --> IDamageable : damages target
    Bullet --> IBulletModifier : applies modifiers
    HomingModifier ..|> IBulletModifier
    PierceModifier ..|> IBulletModifier
    SplitModifier ..|> IBulletModifier

    EnemySpawnerSystem --> EnemyController : spawns
    EnemySpawnerSystem --> EnemySpawnConfig : reads spawn rules
    EnemySpawnerSystem --> RunPressureConfig : reads pressure curve
    EnemySpawnerSystem --> PoolSystem : reuses enemies
    EnemyController ..|> IDamageable
    EnemyController ..|> IConditionalDamageable
    EnemyController ..|> IPoolable
    EnemyController --> PlayerUnit : attacks player

    GateSystem --> GateLogic : creates gate choices
    GateSystem --> GateRuntimeEffectController : applies timed effects
    GateSystem --> GatePoolConfig : reads gate pool
    GateLogic --> GateConfig : displays and applies config
    GateLogic ..|> IPoolable
    GateEffectApplier --> GateConfig : reads effect
    GateEffectApplier --> PlayerController : modifies squad
    GateEffectApplier --> PlayerUnit : modifies stats

    StoryCutsceneRuntimeController --> StoryCutsceneDirector : starts playback
    StoryCutsceneRuntimeController --> StoryCutsceneUnlockRules : checks unlocks
    StoryCutsceneRuntimeController --> SaveService : reads and records seen ids
    StoryCutsceneDirector --> UISystem : overlays story UI

    SaveService *-- SaveData : owns runtime save
    UISystem --> SaveService : shows wallet and upgrades
    RunStatsTracker --> SaveService : commits run result
```

## Layered Architecture

```mermaid
flowchart TB
    subgraph Presentation["Presentation Layer"]
        UI["UISystem\nMain menu, HUD, pause, upgrades, game over"]
        CutsceneUI["Cutscene UI\nCutsceneDemoUIView / runtime canvas"]
        HealthBars["WorldHealthBarView\nPlayer and enemy health bars"]
    end

    subgraph Application["Application / Core Layer"]
        GM["GameManager\nMain run orchestration"]
        GSM["GameStateMachine\nBootstrap, MainMenu, Playing, Cutscene, Paused, GameOver"]
        StoryRuntime["StoryCutsceneRuntimeController\nInitial and post-run story flow"]
    end

    subgraph Gameplay["Gameplay Domain Layer"]
        Player["PlayerController + PlayerUnit\nMain unit, followers, movement, squad firing"]
        Combat["BulletSpawner + Bullet + Modifiers\nProjectile damage and special bullet behavior"]
        Enemies["EnemySpawnerSystem + EnemyController\nEnemy pacing, movement, damage, rewards"]
        Gates["GateSystem + GateLogic + GateRuntimeEffectController\nGate offers and runtime upgrades"]
        Stats["RunStatsTracker\nSurvival time, kills, coins, score"]
    end

    subgraph Data["Data & Persistence Layer"]
        Configs["ScriptableObject configs\nBalanceBootstrapConfig, GateConfig, GatePoolConfig,\nPlayerUnitConfig, EnemySpawnConfig, RunPressureConfig"]
        Save["SaveService + SaveData\nLocal save, cloud provider abstraction, upgrades, cutscene flags"]
        Rules["Static rules and math\nStoryCutsceneUnlockRules, PlayerMetaUpgradeService, BalanceV1Math"]
    end

    subgraph UnityInfra["Unity / Infrastructure Layer"]
        Unity["Unity Engine\nMonoBehaviour lifecycle, scenes, prefabs, transforms, physics"]
        Assets["Project assets\nSprites, animations, ScriptableObject assets, UI prefabs"]
        Pool["PoolSystem\nReusable bullets, enemies, gates, effects"]
        InputCamera["Input and camera\nInput System, gameplay camera, viewport placement"]
    end

    UI --> GM
    CutsceneUI --> StoryRuntime
    GM --> GSM
    GM --> StoryRuntime
    GM --> Player
    GM --> Enemies
    GM --> Gates
    GM --> Stats
    GM --> Save
    GM --> Configs

    StoryRuntime --> Rules
    StoryRuntime --> Save
    StoryRuntime --> CutsceneUI

    Player --> Combat
    Combat --> Enemies
    Enemies --> Player
    Gates --> Player
    Gates --> Enemies
    Stats --> Enemies
    Stats --> Save

    Player --> Configs
    Enemies --> Configs
    Gates --> Configs
    Combat --> Rules
    UI --> Save

    Gameplay --> Pool
    Presentation --> Unity
    Application --> Unity
    Gameplay --> Unity
    Data --> Assets
    Configs --> Assets
    Pool --> Unity
    InputCamera --> Unity
```

## Notes For Report

- `GameManager` la trung tam dieu phoi vong choi: bat dau run, pause/resume, xu ly game over va kich hoat cutscene.
- Gameplay duoc chia thanh cac module nho: Player, Enemy, Combat, Gate, Stats. Moi module co trach nhiem rieng va duoc `GameManager` ket noi lai.
- Data runtime duoc tach ra khoi logic bang `ScriptableObject`, giup can bang game ma khong can sua code.
- `SaveService` quan ly tien trinh nguoi choi, nang cap, ket qua run va cac cutscene da xem.
- `StoryCutsceneRuntimeController` ket noi he thong cutscene vao game chinh bang cach doc `SaveData`, kiem tra unlock rules va yeu cau `StoryCutsceneDirector` phat cutscene.
