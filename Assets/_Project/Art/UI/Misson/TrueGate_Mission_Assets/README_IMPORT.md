# Mission UI Asset Import Guide

Copy PNG files to:

```text
Assets/_Project/Art/UI/MissionSystem/
```

Unity settings:

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Filter Mode: Point
- Compression: None
- Mip Maps: Off
- Alpha Is Transparency: On
- Wrap Mode: Clamp

Recommended:

- `mission_panel_9slice_128.png`: Image Type = Sliced, border 24/24/24/24.
- Keep mission title and progress as TextMeshPro, not baked into sprites.
- Use `mission_button_alert_128x160.png` while a new mission is unread.
- Switch to `mission_button_normal_128x160.png` after the Mission Log is opened.
- Use `mission_button_complete_128x160.png` briefly for completion feedback.
