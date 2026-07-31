# Android Build Verification - Beta 0.1.1

Verification date: 2026-07-31

Unity: 6000.4.2f1

Release source commit: `29bc438a949089cec249afdc61f4b94b3d320a03`

## Build Configuration

| Setting | Verified value |
|---|---|
| Enabled scenes | `Assets/_Project/Scenes/Main.unity` |
| Application ID | `com.mimicompany.truegate` |
| Product name | `True Gate` |
| Version | `0.1.1` |
| Version code | `2` |
| Scripting backend | IL2CPP |
| Architectures | ARM64 only (`arm64-v8a`) |
| Minimum Android | API 25 (Android 7.1) |
| Target Android | API 36 |
| Orientation | Portrait only |
| Texture subtarget | ETC2 |
| Development build | No |

## Artifact

| Artifact | Size | SHA-256 | Result |
|---|---:|---|---|
| `Builds/Android/TrueGate-Beta-0.1.1-vc2.apk` | 69,737,769 bytes | `0454614E4C4856D126AC6688F2A4034ABD539E579DFC160B38AA4C238CBB7684` | Build and package inspection succeeded |

`aapt` reports package version `0.1.1 (2)`, minimum API 25, and target API 36. APK inspection found only `arm64-v8a` native libraries. `apksigner verify` passes with APK Signature Scheme v2. This local beta APK is signed with the Android debug certificate; a private release keystore is still required before Play Store or public production distribution.

## Automated Verification

- C# compilation: 0 errors.
- Mission fixture: 15/15 passed.
- EditMode: 213/213 passed, 0 failed, 0 skipped.
- PlayMode: 41/41 passed, 0 failed, 0 skipped.
- Total: 254/254 passed.
- Detailed jobs and debt resolution: `Documentation/QA/2026-07-31_Beta_0.1.1_QA_Rerun.md`.

## Device And Migration Verification

Device: OPPO CPH2059 (`a9fcb81`), Android 11 / API 30, ARM64, 1080 x 2400.

- Installed over Beta 0.1.0 code 1 with `adb install -r`; package became Beta 0.1.1 code 2.
- `firstInstallTime` remained `2026-07-31 20:42:07`, confirming an update rather than a clean install.
- The original `save.json` SHA-256 remained `515544E7812F2471E3537E1AEECFD9613BA3E5FF094FFC374D24C196F5AE7798` before install, after install, and after first 0.1.1 launch.
- The original `save.bak` SHA-256 remained `0ACCAF8DF6173CBE3E534B7C69A857DDBDB87D554D9D8F92CAB260D9D0BA45BA` across the install-over check.
- The app launched to the preserved Loop 0 state with no fatal Java exception, native fatal signal, or ANR attributed to `com.mimicompany.truegate` in the captured logcat. Two unrelated `com.android.shell` exceptions were caused by the OPPO firmware rejecting the ADB `svc power stayon` command.

## Final Choice Smoke Test

A temporary migration fixture was used on the physical device and the original save was restored byte-for-byte afterward.

- A save at Loop 100 loaded successfully without reset.
- A run survived `602.616394` seconds and changed `totalRunsCompleted` from 100 to 101.
- Final Choice appeared, proving saves above 50 runs remain eligible.
- `CONTINUE PROTOCOL` opened and completed `CS_07_FinalChoice_ContinueProtocol`.
- The resulting save had `finalChoiceResolved = true`, recorded both Final Choice cutscene IDs, and completed `break_final_choice`.
- The first Terminal Protocol progress entries, `terminal_1000_kills_run` and `terminal_10000_total_kills`, were created; the Mission Log displayed the Terminal Protocol chain with later entries locked by their sequential prerequisites.
- The original save was restored, relaunched, and reverified with the same SHA-256.

## Known Issues

- The candidate uses a debug signing certificate and is suitable for local beta sideloading only. Production distribution still requires the project's private release keystore.
- The ending smoke test used a temporary Loop 100 fixture to reach the narrative gate deterministically. The fixture was removed and the user's original save was restored exactly after verification.
