# Cutscene Demo Setup Checklist

This demo scene is intentionally isolated from `Main.unity`.

## Current Demo Behavior

- Open `Assets/_Project/Scenes/CutsceneDemo.unity`.
- Press any of the 7 demo buttons.
- Each button calls `StoryCutsceneDirector.Play(...)`.
- If no Easy Cutscene entry is wired, the fallback dialogue panel runs the scripted dialogue from `StoryCutsceneLibrary`.
- Each dialogue line shows speaker, emotion, and body text.
- `NEXT` advances one dialogue line.
- `CLOSE` closes the active cutscene.
- `CS_07_FinalChoice` runs `CS_07_FinalChoice_PreChoice`, then shows:
  - `CONTINUE PROTOCOL`
  - `SHUT DOWN CORE`
- Each CS7 choice plays its matching branch dialogue.

## Future Easy Cutscene Wiring

1. Add or instantiate the Easy Cutscene manager prefab under `CutsceneDemoRoot/CutsceneManager`.
2. Configure Easy Cutscene entries with names matching:
   - `CS_01_BootSequence`
   - `CS_02_FirstDeathRecovery`
   - `CS_03_EnemyDoesNotCharge`
   - `CS_04_GateMemoryLeak`
   - `CS_05_HumanCommand`
   - `CS_06_SystemFatigue`
   - `CS_07_FinalChoice_PreChoice`
   - `CS_07_FinalChoice_ContinueProtocol`
   - `CS_07_FinalChoice_ShutDownCore`
3. Assign the Easy Cutscene manager reference on `StoryCutsceneDirector`.
4. Keep the fallback dialogue UI assigned so missing entries log a warning instead of crashing.
