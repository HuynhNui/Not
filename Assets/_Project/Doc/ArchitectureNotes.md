# Architecture Notes

## Runtime Ownership

- `Core/GameLoop/GameManager` owns run-level flow such as start, game over, pausing, and wiring major systems together.
- `Gameplay/*` components own local behavior only. For example, a player unit can die and emit an event, but it should not directly show UI or stop enemy spawning.
- `Systems/*` components own cross-object features such as enemy spawning, UI state, gates, levels, and pooling.
- `Data/ScriptableObjects/*` stores tunable gameplay values that should not live only in scenes.

## Scene Rules

- The main playable scene is `Assets/_Project/Scenes/Main.unity`.
- A playable scene should contain one `GameManager`.
- UI screens that designers need to edit should live in the scene or in prefabs, not be generated from gameplay scripts.
- Scene references should be serialized where practical. Runtime object search is acceptable as a fallback, not as the primary dependency path.

## UI Rules

- Game-over UI is `GameCanvas/GameOverPanel`.
- `UISystem` is responsible for showing and hiding HUD and game-over views.
- UI button callbacks should be wired by UI/system scripts, not by gameplay units.

## Data Rules

- Player tuning lives in `Assets/_Project/Data/Player`.
- Enemy unit tuning lives in `Assets/_Project/Data/Enemies`.
- Spawn pacing lives in `Assets/_Project/Data/Spawning`.

## Input Rules

- This project uses the Input System package.
- EventSystems must use `InputSystemUIInputModule`, not `StandaloneInputModule`.
- `EventSystemInputSystemBootstrap` exists as a runtime guard for old scenes or prefabs that still contain the legacy module.
