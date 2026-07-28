# Flick Sort

A portrait hyper-casual poker-chip sorting prototype made for the XOAI Game Developer Prototype Challenge, **Option 3: Flick Sort**.

The player taps one stack and then another to move a contiguous top group of matching-color chips. Filling a group of ten merges it into a higher-level chip, awards progress, and drives an endless level sequence with stack and chip-level unlocks.

## Submission documents

- [Process Plan](Submission/PROCESS_PLAN.md)
- [Art / Asset Requirements](Submission/ART_ASSET_REQUIREMENTS.md)
- Third-party license records: [`Assets/Game/License`](Assets/Game/License)

## Current delivery status

| Deliverable | Status |
|---|---|
| Process plan | Complete |
| Art / asset requirements | Complete |
| Unity source code | Complete for review |

## Requirements

- Unity **6000.3.13f1** (Unity 6.3 LTS).
- Universal Render Pipeline **17.3.0**.
- Input System **1.19.0**.
- TextMeshPro / uGUI.
- DOTween is included under `Assets/Plugins/Demigiant`.
- Android is the current target platform; the game also runs in the Unity Editor.

Opening the project with the exact Unity version is recommended to avoid asset or serialization upgrades.

## How to run in Unity

1. Clone or extract the repository.
2. Open the repository root in Unity Hub using Unity `6000.3.13f1`.
3. Allow Unity to import packages and compile scripts.
4. Open `Assets/Scenes/FlickSort.unity`.
5. Enter Play Mode.

The game opens through `LoadingUI`, initializes the board, and then immediately enters gameplay. No tutorial or save data is required.

> `Assets/Scenes/FlickSort.unity` is the submission scene. `SampleScene.unity` is a template scene and should not be used to review gameplay.

## Controls

- **Move chips:** tap a non-empty source stack, then tap a destination stack.
- A destination is legal when it is empty or its accessible chip color matches the moving group.
- If the destination has fewer free slots than the source group, only the amount that fits is moved.
- **Deal:** tap `DEAL` to add chips from the authored chip spawner.
- **Shuffle:** rearranges chips across available stacks, favoring adjacent same-color groups without granting a free merge immediately.
- **Hammer:** activate the skill, then tap a stack to destroy its chips and score one point per destroyed chip.
- **Level Up:** tap the level-up overlay to continue. The board is preserved and progress resets for the new level.
- **Lose / Retry:** when all available slots are full, retry restarts the same level.

## Gameplay rules

- Default stack capacity: 10 chips.
- Merge requirement: 10 matching chips.
- Maximum chip level: 10; merging level 10 keeps it at level 10.
- Score is based on the number of chips consumed or destroyed, not the number of merge events.
- New chip levels enter the random deal pool after being unlocked.
- Levels can increase required score, deal count, and the number of available stack slots.
- The current version intentionally has no persistence/save system.

Core balancing values are stored in:

- `Assets/Game/Data/FlickSortGameConfigSO.asset`
- `Assets/Game/Data/ChipColorConfigSO.asset`

## Project structure

| Path | Purpose |
|---|---|
| `Assets/Scenes/FlickSort.unity` | Main submission scene |
| `Assets/Game/Scripts/Runtime/Core` | Board, stack, chip, event, sound, feedback, bootstrap and UI controllers |
| `Assets/Game/Scripts/Runtime/Core/UI` | UI base classes, manager, screens and animations |
| `Assets/Game/Data` | ScriptableObject configuration and UI definitions |
| `Assets/Game/Prefabs/Gameplay` | Chip and 20-slot tray prefabs |
| `Assets/Game/Prefabs/UI` | Loading, Gameplay, Level Up and Lose UI prefabs |
| `Assets/Game/Prefabs/VFX` | Reusable merge and shadow effects |
| `Assets/Game/3D` | Models, materials and textures |
| `Assets/Game/Sprite` | Background, skill icons and UI/VFX sprites |
| `Assets/Game/Sound` | Runtime music and sound effects plus source packs |
| `Assets/Game/License` | License and attribution records |
| `Assets/Game/Tests/EditMode` | Core stack-model tests |

## Architecture overview

- `FlickSortBootstrap` initializes UI, board, sound and loading flow.
- `FlickSortBoard` owns gameplay sequencing, input, dealing, movement, merging, skills, pooling, scoring and level progression.
- `ChipStackModel` contains deterministic stack rules separated from the scene representation.
- `ChipStackView` owns authored stack transforms, colliders, availability panels and feedback.
- `ChipView` owns chip rendering, label changes, trails and DOTween motion.
- `FlickSortEventBus` decouples gameplay events from UI, sound and VFX.
- `UIManager` instantiates registered UI prefabs and controls show/hide behavior.
- Runtime chip objects and merge particles are reused to reduce allocation and object churn.

## Responsive layout

- UI uses a `1080 × 1920` portrait reference with `Scale With Screen Size`.
- Gameplay HUD respects `Screen.safeArea`.
- On narrow/tall displays, the orthographic camera zooms out while keeping the tray in the same screen region.
- The tray transform is never scaled for responsiveness, preserving chip spacing and collider alignment.
- The felt background scales to cover the responsive camera.

## Validation performed

- Unity Editor compilation completed without new C# errors after the latest gameplay and optimization changes.
- The latest recorded EditMode run completed with **12 passed, 0 failed, 0 skipped**.
- Play Mode was checked for initialization and runtime exceptions.
- Responsive framing was checked in Unity Simulator at `1440 × 3088` as well as the portrait reference view.

## Credits and licenses

Detailed records and source URLs are stored in [`Assets/Game/License`](Assets/Game/License).

- UI and selected casino/impact sounds by [Kenney](https://kenney.nl/), CC0.
- Collect Points sound by LIECIO from Pixabay, Pixabay Content License.
- Casino BGM by MFCC, level-up sound by PuyoPuyoMegaFan1234, and game-over sound by Tuomas_Data, Pixabay Content License.
- Shuffle sound by OxidVideos and Hammer coin-clatter sound by PWLPL, Pixabay Content License.
- Sunburst effect by simranzenov via OpenGameArt, CC0.
- Green felt background by jbp4444 via OpenGameArt, CC0.
- Poker chip tray by RandomRust, CC BY-SA.
- Wooden Planks texture by Charlotte Baglioni and Dario Barresi via Poly Haven, CC0.
- Shuffle and Hammer icons by Delapouite via Game-icons.net, CC BY 3.0.

### Distribution note

The imported poker-chip model and optional Coins, Casual Hit, SimpleFX and LanaStudio packs do not have complete license records bundled locally. Before a public build or redistribution, verify their original licenses and remove unused third-party packs where appropriate. This does not prevent source review, but it must be resolved before public release.
