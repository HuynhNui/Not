# UI Safe Area Audit - Before

| Scene/Prefab | Canvas Path | Render Mode | Safe Area Needed | Full-screen Background | Status |
|---|---|---|---:|---|---|
| Assets/_Project/Prefabs/UI/WorldHealthBar_New.prefab | WorldHealthBar_New | WorldSpace | No | Review hierarchy | Excluded: world-space |
| Assets/_Project/Scenes/CutsceneDemo.unity | CutsceneDemoRoot/DemoCanvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/_Project/Scenes/Main.unity | GameCanvas | ScreenSpaceOverlay | Yes | Review hierarchy | Partial/Present: GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel; GameCanvas/UIRoot/SafeAreaRoot/PausePanel; GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel/GameOverContentFrame |
| Assets/_Project/Scenes/Main.unity | GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel | WorldSpace | No | Review hierarchy | Excluded: world-space |
| Assets/_Project/Scenes/Main.unity | GameCanvas/UIRoot/SafeAreaRoot/UpgradePanel | WorldSpace | No | Review hierarchy | Excluded: world-space |
| Assets/2D Health & Damage System asset/Scenes/Demo.unity | Canvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/SCENES/DEMO.unity | Canvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/Easy Cutscene/Demo/Scenes/Full Demo Scene.unity | Canvas | ScreenSpaceCamera | Yes | Review hierarchy | Missing |
| Assets/Easy Cutscene/Demo/Scenes/Full Demo Scene.unity | Canvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/Easy Cutscene/Demo/Scenes/Sample Demo.unity | Canvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/Scenes/SampleScene.unity | GameCanvas | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/Scenes/SampleScene.unity | GameCanvas/GameOverPanel | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |
| Assets/Scenes/SampleScene.unity | GameCanvas/HUDRoot | ScreenSpaceOverlay | Yes | Review hierarchy | Missing |

## Runtime UI Search

Runtime-created Canvas and panel hierarchies must be validated after code search and smoke testing. World-space canvases are intentionally excluded from safe-area fitting.
