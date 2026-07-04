Implement a responsive Unity UI screen/popup named `SettingsPanel` for the mobile portrait game True Gate.

Use the provided transparent PNG UI elements from `setting_panel_elements`:
- `setting_main_panel_9slice_source.png`
- `setting_row_card_9slice_source.png`
- `back_button.png`
- `toggle_on.png`
- `toggle_off.png`
- `reset_button_bg.png`
- `icon_gear.png`
- `icon_music.png`
- `icon_sfx.png`
- `icon_vibration.png`
- `icon_damage_text.png`
- `icon_reset.png`

Goal:
Create a simple in-game settings panel matching the existing cute sci-fi pixel UI style. The screen must work on different mobile portrait aspect ratios without clipping or layout drift.

Do not hard-code positions for one simulator resolution. Use anchors, safe area, layout groups, and scalable 9-sliced UI images.

Canvas setup:
- Canvas Render Mode: Screen Space - Overlay, or match the existing project canvas.
- CanvasScaler:
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1080 x 1920
  - Screen Match Mode: Match Width Or Height
  - Match: 0.5
- Put the entire panel under a `SafeAreaRoot` RectTransform.
- If the project already has a SafeArea script, use it. If not, create a reusable SafeAreaFitter script.
- `SettingsPanel` should be disabled by default and shown from the main menu/settings button.

Hierarchy:
SettingsPanel
Overlay
SafeAreaRoot
MainPanel
Header
BackButton
TitleGroup
GearIcon
TitleText
RowsContainer
MusicRow
SfxRow
VibrationRow
DamageTextRow
ResetButton

Overlay:
- Fullscreen Image.
- Anchor min `(0,0)`, anchor max `(1,1)`.
- Offsets all zero.
- Color: dark navy/black with alpha around 0.35 to 0.55.
- Blocks raycasts so gameplay/main menu behind it cannot be tapped.

SafeAreaRoot:
- Anchor min `(0,0)`, anchor max `(1,1)`.
- Offsets updated by SafeAreaFitter.

MainPanel:
- Use `setting_main_panel_9slice_source.png` as Image.
- Image Type: Sliced.
- Preserve pixel corners; do not stretch as a simple Image if slicing is available.
- Anchor preset: middle center.
- Pivot: `(0.5, 0.5)`.
- Width: 82 to 88 percent of SafeAreaRoot width.
- Height: content-driven, around 56 to 68 percent of SafeAreaRoot height.
- Add `VerticalLayoutGroup`:
  - Padding top around 56, bottom around 48, left/right around 64.
  - Spacing around 32.
  - Child Alignment: Upper Center.
  - Control Child Size Width: true.
  - Control Child Size Height: false.
  - Child Force Expand Width: true.
  - Child Force Expand Height: false.
- Add `ContentSizeFitter` only if needed; prefer a fixed responsive RectTransform size plus layout children.
- Add a `LayoutElement` with preferred height, but keep it below SafeAreaRoot height.

Header:
- HorizontalLayoutGroup.
- Height around 104 px at reference resolution.
- Width should fill MainPanel.
- Contains BackButton on the left, TitleGroup centered, and a right spacer matching BackButton width.
- This keeps the title visually centered.

BackButton:
- Use `back_button.png`.
- Anchor handled by Header layout group.
- Size: 72 to 96 px depending on panel scale.
- Button target graphic: back image.
- On click: hide SettingsPanel or return to previous menu state.

TitleGroup:
- HorizontalLayoutGroup.
- Child Alignment: Middle Center.
- Spacing: 24.
- Contains GearIcon and TitleText.
- GearIcon uses `icon_gear.png`, size 56 to 72 px.
- TitleText:
  - Text: `SETTING` or `SETTINGS`.
  - Use the project's pixel font / TextMeshPro.
  - Color: dark navy.
  - Font size responsive, around 54 to 68 at 1080 x 1920.
  - Enable Auto Size only within a safe min/max range if needed.

RowsContainer:
- VerticalLayoutGroup.
- Width fills MainPanel.
- Spacing around 28 to 36.
- Child Force Expand Width: true.
- Contains four rows only:
  1. MUSIC
  2. SFX
  3. VIBRATION
  4. DAMAGE TEXT

Each row:
- Use `setting_row_card_9slice_source.png` as the row background Image.
- Image Type: Sliced.
- Height: around 112 to 132 px at reference resolution.
- Width: fill parent.
- Add HorizontalLayoutGroup:
  - Padding left/right around 42.
  - Spacing around 28.
  - Child Alignment: Middle Center.
  - Control Child Size Width: false.
  - Control Child Size Height: false.
- Structure:
  - Icon Image, fixed size 64 to 76 px.
  - Label Text, flexible width.
  - Toggle Button/Image, fixed size around 164 x 72 px.
- Label Text:
  - Use TextMeshPro with pixel font.
  - Dark navy.
  - Left aligned, vertically centered.
  - Auto Size with a safe minimum for `DAMAGE TEXT` on narrow devices.
- Toggle:
  - Use `toggle_on.png` and `toggle_off.png`.
  - Treat it as a Button or Toggle component.
  - Swap sprite based on state.
  - Music and SFX are ON/OFF toggles, not sliders.
  - Do not use volume sliders in this panel.

Row details:
- MusicRow icon: `icon_music.png`, label `MUSIC`, toggle controls music enabled.
- SfxRow icon: `icon_sfx.png`, label `SFX`, toggle controls sound effects enabled.
- VibrationRow icon: `icon_vibration.png`, label `VIBRATION`, toggle controls vibration enabled.
- DamageTextRow icon: `icon_damage_text.png`, label `DAMAGE TEXT`, toggle controls floating damage number visibility.

ResetButton:
- Use `reset_button_bg.png` as Image.
- Image Type: Sliced if possible.
- Place below RowsContainer inside MainPanel.
- Anchor/layout: centered by MainPanel VerticalLayoutGroup.
- Size around 320-380 px wide and 68-84 px high at reference resolution.
- Text child: `RESET DATA`.
- Optional icon child: `icon_reset.png` aligned right.
- Style as secondary action, smaller than gameplay primary buttons.
- On click: open a confirmation popup before resetting data. Do not reset immediately.

Responsive behavior:
- MainPanel should remain centered inside SafeAreaRoot.
- On tall screens, vertical spacing may expand slightly.
- On short screens, reduce MainPanel padding and RowsContainer spacing before shrinking text.
- Do not clip the reset button on short devices.
- If height is too tight, make MainPanel slightly taller up to safe area limits.
- Do not let any element overlap the notch, rounded corners, or home indicator.
- Test in Unity Simulator:
  - iPhone notch portrait
  - tall Android portrait
  - 16:9 Android portrait
  - small portrait resolution

Import settings for all PNG assets:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Filter Mode: Point (no filter)
- Compression: None or High Quality
- Generate Mip Maps: off
- Pixels Per Unit can match the existing UI asset convention.
- For panel, row, and button backgrounds, set borders for 9-slicing in Sprite Editor.

Controller script:
- Create `SettingsPanelUI`.
- Serialized references:
  - root GameObject
  - music Toggle/Button
  - sfx Toggle/Button
  - vibration Toggle/Button
  - damageText Toggle/Button
  - back Button
  - reset Button
- Public methods:
  - `Show()`
  - `Hide()`
  - `SetMusicEnabled(bool enabled)`
  - `SetSfxEnabled(bool enabled)`
  - `SetVibrationEnabled(bool enabled)`
  - `SetDamageTextEnabled(bool enabled)`
- The UI should only display/update settings values.
- Store settings through the existing settings/save system if available. If not available, use PlayerPrefs with clear keys.

Important:
- Do not add Save, Credit, Language, or Graphics controls.
- Do not add sliders.
- Do not build a marketing screen.
- Build the actual usable Unity UI panel.
