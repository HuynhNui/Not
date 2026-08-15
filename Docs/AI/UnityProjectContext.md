# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/Artermis/My project`
- Product: True Gate, a portrait-oriented 2D survival game built around squad combat and gate choices.
- Last analyzed: 2026-08-14
- Last analyzed commit: `d6954fe`

## Confirmed Environment

- Unity version: 6000.4.2f1 (`7a4c1aeef971`)
- Render pipeline: Universal Render Pipeline 17.4.0, using the 2D renderer
- Input system: Unity Input System 1.8.0; project action asset at `Assets/InputSystem_Actions.inputactions`
- Active build target: Android
- Android scripting backend: IL2CPP
- Color space: Linear

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.4.0 with Unity 2D packages | Confirmed | `Packages/manifest.json`, connected Editor project info |
| Input | New Input System 1.8.0 | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions` |
| UI | uGUI and TextMeshPro | Confirmed | package manifest and first-party UI/gameplay scripts |
| Tests | Unity Test Framework 1.6.0 | Confirmed | `Packages/manifest.json`, first-party test folders |
| Editor automation | AnkleBreaker Unity MCP is installed and connected | Confirmed | `Packages/manifest.json`, live Editor connection on port 7890 |
| Networking | No first-party multiplayer runtime detected | Likely | package/code inspection |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/_Project/Scripts/Core` | Run state and high-level game orchestration | Confirmed | `GameManager.cs`, state-machine code |
| `Assets/_Project/Scripts/Systems` | Feature systems such as gates, enemies, UI, saves, missions, audio, and telemetry | Confirmed | folder/code inspection |
| `Assets/_Project/Scripts/Gameplay` | Runtime entities and presentation components | Confirmed | gate, player, enemy, projectile code |
| `Assets/_Project/Scripts/Data` | Balance data and ScriptableObject definitions | Confirmed | folder/code inspection |
| `Assets/_Project/Data` | Authored ScriptableObject instances and balance profiles | Confirmed | asset inspection |
| `Assets/_Project/Art` | First-party sprites and visual source assets | Confirmed | asset inspection |
| `Assets/_Project/Prefabs` | First-party runtime prefabs | Confirmed | asset inspection |
| `Assets/_Project/Tests` | EditMode and PlayMode tests | Confirmed | test files and PlayMode asmdef |
| `Assets/_Project/Editor` | Project-specific editor and mobile asset tooling | Confirmed | editor script inspection |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | Main first-party runtime gameplay | UnityEngine, URP/2D, TMPro, Input System | Most runtime code is in the default assembly |
| `Assembly-CSharp-Editor` | Editor tooling and EditMode tests | UnityEditor, production assembly | No first-party runtime asmdef was found |
| `TrueGate.PlayModeTests` | PlayMode integration tests | TestAssemblies, production assembly | Defined by `Assets/_Project/Tests/PlayMode/TrueGate.PlayModeTests.asmdef` |

## Scenes And Startup Flow

- Enabled build scenes: `Assets/_Project/Scenes/Main.unity` only.
- Startup scene: `Main` at build index 0.
- The connected Editor also had `Main` open and clean during onboarding.
- `GameManager` is the scene-level composition/orchestration point for run state, gameplay systems, tutorial, missions, UI, save, audio, and telemetry.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Scene-composed MonoBehaviours | Systems are wired with private serialized references and initialized from scene/prefab owners | Confirmed | `GameManager.cs`, `GateSystem.cs`, `GateDoor.prefab` |
| Feature-oriented systems | Runtime responsibilities are grouped under `Scripts/Systems` and `Scripts/Gameplay` | Confirmed | first-party directory structure |
| ScriptableObject configuration | Gate, balance, economy, mission, and other authored data are asset-backed | Confirmed | `Scripts/Data`, `Assets/_Project/Data` |
| Runtime fallback resolution | Some components use `FindAnyObjectByType` or add missing helpers during `Init()` | Confirmed | `GameManager.cs`, `GateSystem.cs`, `DoorView.cs` |
| Object pooling | Enemies, projectiles, gates, and related objects use a project pool system | Confirmed | `PoolSystem` references and gameplay scripts |

## Coding Conventions

- Namespaces follow `_Project.Scripts.<Area>...` for most runtime code.
- Inspector fields are normally `[SerializeField] private`; mutable runtime state is private with public read-only properties where needed.
- Public initialization commonly uses idempotent-looking `Init()` methods, often also called from `Awake()`.
- Braces use Allman style; private fields generally use `_camelCase`, serialized fields generally use `camelCase`.
- XML summaries are used for important systems and public-facing gameplay components.
- Async work follows the existing service pattern; no third-party async framework was detected.

## Testing And Validation

- EditMode tests exist under `Assets/_Project/Tests/Editor`.
- PlayMode tests exist under `Assets/_Project/Tests/PlayMode` in `TrueGate.PlayModeTests`.
- Android is the active build target; the normal release artifact is an APK built from `Main.unity`.
- No repository CI workflow was found during this inspection.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Editor connection/version/state | available | live AnkleBreaker Unity MCP instance for `True Gate` |
| Console read and compilation errors | available | live MCP console/compilation probes |
| Scene/build-settings inspection | available | live MCP scene and project-info probes |
| GameObject/prefab/component inspection | available | live MCP tool set |
| Asset search/import inspection | available | Unity AssetDatabase search through MCP |
| Tests and Play Mode | available | Unity MCP advanced tools and play-mode control |
| Android player build | available | Unity MCP build capability |
| Runtime/editor screenshots | available | Unity MCP game/editor capture capabilities |
| Profiler capture | unverified | not exercised during onboarding |

## Important Constraints

- Preserve uncommitted user changes; the working tree was already dirty at onboarding.
- Treat scenes, prefabs, ScriptableObjects, sprite atlases, texture metadata, and Project Settings as high-impact serialized files.
- `Assets/_Project/Scenes/Main.unity` is the only enabled release scene.
- Gate visuals are driven by `GateSpriteLibrary.asset`, `GateDoor.prefab`, `DoorView.cs`, and `Gameplay_Gates.spriteatlas`.

## Unknowns And Confidence

- Release signing/key configuration was not inspected.
- CI and store-distribution configuration remain unknown.
- Performance budgets and supported Android device matrix were not documented in the inspected sources.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Assets/_Project/Scenes/Main.unity` (Editor/build-settings metadata only)
- `Assets/_Project/Scripts/Core/GameLoop/GameManager.cs`
- `Assets/_Project/Scripts/Systems/GateSystem/GateSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Gates/DoorView.cs`
- `Assets/_Project/Scripts/Data/ScriptableObjects/GateConfigs/GateSpriteLibrary.cs`
- `Assets/_Project/Prefabs/Gates/GateDoor.prefab`
- `Assets/_Project/Tests/PlayMode/TrueGate.PlayModeTests.asmdef`
- `Docs/Architecture_Diagrams.md`
- Connected Unity Editor project, scene, build-settings, asset-search, and Console data

<!-- unity-onboarding:generated:end -->
