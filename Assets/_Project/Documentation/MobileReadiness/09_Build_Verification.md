# Android Build Verification

## Build Configuration

| Setting | Verified value |
|---|---|
| Enabled scenes | `Assets/_Project/Scenes/Main.unity` |
| Application ID | `com.mimicompany.truegate` |
| Product name | `True Gate` |
| Version | `0.1.0` |
| Version code | `1` |
| Scripting backend | IL2CPP |
| Architectures | ARM64 |
| Minimum Android | API 25 (Android 7.1) |
| Target Android | API 36 |
| Orientation | Portrait only |
| Texture subtarget | ETC2 |

## Artifacts

| Artifact | Type | Size | SHA-256 | Result |
|---|---|---:|---|---|
| `Builds/Android/TrueGate-Beta-0.1.0-vc1-dev.apk` | Development | 82,684,764 bytes | `67CE9F823430FDC2D8F48BCA187F17079E47B4DBE8111E87D0F78F7EFD0F246F` | Build succeeded |
| `Builds/Android/TrueGate-Beta-0.1.0-vc1.apk` | Release candidate | 69,741,069 bytes | `F9C52D94BC56CB1A66EC485655A6EB7B77A6CE82DD0A667A41D2CA3DF14B2420` | Build succeeded |

Both APKs pass `apksigner verify` with APK Signature Scheme v2. The local beta APK uses the Android debug certificate; a private release keystore is still required before Play Store or public production distribution.

## Device Verification

Device: OPPO CPH2059, Android 11 / API 30, ARM64, 1080 x 2400, 8 GB class RAM.

- Installed both APKs with `adb install -r`; save data was preserved.
- RC launched with process alive and package version `0.1.0 (1)`.
- Main menu, gameplay HUD, tutorial skip, pause, and settings were exercised by touch input.
- Music and SFX toggles changed runtime state and remained OFF after force-stop, relaunch, dev-to-RC upgrade, and another settings open.
- No fatal exception, native fatal signal, or package ANR was present in the captured logcat.
- RC settings-screen memory snapshot: total PSS 372,576 KB; total RSS 490,640 KB.
- Battery temperature moved from 31.5 C before the extended build/test session to 35.3 C while USB powered; no thermal shutdown or app termination occurred.
- Android `gfxinfo` did not expose useful Unity Surface frame percentiles on this device, so no unsupported FPS claim is made.

## Automated Verification

- C# compilation: 0 errors.
- Missing references: 0 in the build scene and 0 in project assets.
- EditMode: 210 passed, 3 known baseline failures.
- PlayMode: 35 passed, 6 known baseline failures; the contact cooldown test passed twice consecutively after the shared collision guard fix.

The remaining failures are pre-existing balance/test-fixture issues and are listed in `10_Mobile_Readiness_Summary.md`.
