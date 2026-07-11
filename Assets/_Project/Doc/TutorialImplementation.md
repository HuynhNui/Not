# True Gate Tutorial Implementation

## Flow

First-time onboarding is split into two independent flows:

1. Gameplay tutorial starts from the first `START RUN` when `gameplayTutorialCompleted` is false.
2. Upgrade tutorial starts after the first Game Over when gameplay tutorial is complete and `upgradeTutorialCompleted` is false.

Gameplay tutorial dialogue uses transient story cutscenes. These cutscenes reuse the existing story UI but do not update `seenCutsceneIds` or `storyStage`.

The gameplay tutorial teaches movement, automatic firing, enemy avoidance, collecting the real `major_recruit` gate, then choosing one gate from the deterministic default set: `stable_damage`, `utility_repair`, and `risky_glass_cannon`.

The upgrade tutorial highlights the Game Over upgrade button, upgrade currency, and a preferred upgrade button. It grants a one-time recovery coin bonus only if needed.

## Save Flags

`SaveData` stores:

- `gameplayTutorialCompleted`
- `upgradeTutorialCompleted`
- `tutorialFirstRunBonusGranted`
- `tutorialVersion`

`SaveService.ResetPlayerProgression()` creates a fresh `SaveData`, so reset data also resets tutorial state.

## Scene Hierarchy

The setup tool creates:

```text
GameCanvas
└── UIRoot
    └── SafeAreaRoot
        └── TutorialOverlayPanel
```

`TutorialManager`, `TutorialGameplayDirector`, and `TutorialUpgradeDirector` are attached to the existing `GameManager` object. The overlay remains available for icon hints, highlights, skip, and upgrade tutorial callouts; gameplay tutorial instructional text comes from transient cutscenes.

## Setup And Replay

Use Unity menu items:

- `Tools/True Gate/Tutorial/Setup Tutorial UI`
- `Tools/True Gate/Tutorial/Apply Sprite Import Settings`
- `Tools/True Gate/Tutorial/Reset Tutorial Flags`
- `Tools/True Gate/Tutorial/Mark Tutorial Complete`

The setup menu is idempotent and can be rerun after UI changes.

## Adding Steps

Add a new value to `TutorialStepId`, add the transient cutscene copy in `TutorialCutsceneDefinitions`, then add the coroutine logic in the relevant director. Keep every step skippable and give gameplay-dependent steps a timeout fallback.

## QA Checklist

- New save reaches Main Menu after boot cutscene.
- First `START RUN` opens gameplay tutorial.
- Tutorial cutscenes do not mark story cutscenes seen.
- Movement, auto-fire, enemy, recruit gate, and default gate choice steps complete without softlock.
- Skip completes and cleans up tutorial objects.
- First Game Over opens upgrade tutorial.
- One upgrade is always affordable.
- Purchase marks upgrade tutorial complete.
- Reset data replays both tutorials.
