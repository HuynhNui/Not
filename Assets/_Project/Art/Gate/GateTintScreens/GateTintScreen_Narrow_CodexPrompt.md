Use the narrow gate tint screen assets instead of the previous wide version.

Replace/import these assets into `Assets/_Project/Art/Gate/GateTintScreens/`:
- `gate_tint_screen_positive_green_narrow_192x160.png`
- `gate_tint_screen_negative_red_narrow_192x160.png`
- optional fallback: `gate_tint_screen_neutral_blue_narrow_192x160.png`

Reason:
The previous tint pane was too wide and visually fought the portal frame. This narrow version keeps the same 192x160 canvas but reduces the visible pane width so it sits inside the gate pillars.

Implementation remains the same:
- Child SpriteRenderer name: `GateTintScreen`
- Parent: gate root / `GateDoor`
- Local position: `(0, 0, smallZ)`
- Local scale: one
- Sorting layer follows `frameRenderer`
- Sorting order: `frameRenderer.sortingOrder - 1`
- Green for `config.IsBuff == true`
- Red for `config.IsBuff == false`
- Do not use UI anchors; this is a world-space gameplay sprite attached to the gate.

If the pane still feels too wide in Unity, do not rescale the whole gate. Only reduce `GateTintScreen.transform.localScale.x` to around `0.9` or use the narrower PNG above. Keep the 192x160 sprite canvas unchanged so pivot/import behavior stays stable.
