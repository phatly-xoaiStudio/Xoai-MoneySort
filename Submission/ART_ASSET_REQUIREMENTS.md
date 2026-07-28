# Flick Sort — Art / Asset Requirements

## Art direction

- **Theme:** bright, friendly casino / poker-chip toy aesthetic.
- **Camera:** portrait orthographic view with a slightly tilted 3D tray.
- **Readability:** every chip level must be distinguishable by color and a large numeric label; stack availability and progress must be readable within a few seconds.
- **Rendering target:** Unity 6 URP on mobile. Repeated chip meshes use shared materials and GPU instancing where supported. Chip shadows, motion vectors, and probes are disabled to keep the board lightweight.
- **UI reference:** 1080 × 1920 portrait, Scale With Screen Size, safe-area aware.

## Production asset list

| Category | Requirement / count | Format and target size | Style / technical notes | Current source |
|---|---:|---|---|---|
| Poker chip model | 1 reusable mesh/prefab | FBX, low-poly; one gameplay prefab | Horizontal chip with numeric TMP label, trail child, shared mesh; supports 10 levels | Imported poker-chip FBX; source license must be verified before public distribution |
| Chip color materials | 10 | Unity `.mat`, URP Lit | Blue, green, purple, red, yellow, orange, black, cyan, pink, grey; shared materials, GPU instancing enabled | Project-authored materials |
| Chip tray | 1 model, 20 logical stack slots | FBX, approximately 608 KB source | 4 × 5 logical stack grid; authored stack roots, colliders and block panels | RandomRust / Thingiverse, CC BY-SA; see `Assets/Game/License/LICENSEPokerChipTray.txt` |
| Tray wood surface | 1 material + 2 textures | 1K JPG diffuse + OpenGL normal | Warm wooden planks, tiled on tray | Poly Haven Wooden Planks, CC0; Charlotte Baglioni and Dario Barresi |
| Background | 1 image | JPG; imported as 2D sprite | Bright green felt, world-space behind the 3D tray; scales to cover camera | Felt Backgrounds by jbp4444, OpenGameArt, CC0 |
| Skill icons | 2 | Transparent PNG; source files under 8 KB each | Shuffle/card exchange and Hammer/break; high contrast for square buttons | Delapouite via Game-icons.net, CC BY 3.0 |
| Level-up glow | 1 sprite | Transparent PNG, approximately 134 KB | Radial yellow sunburst rotating clockwise behind unlocked chip | simranzenov via OpenGameArt, CC0 |
| UI kit | Buttons, panels, progress frame/fill, star and popup elements | PNG sprites, 9-sliced where appropriate | Bright arcade/casual style; readable on portrait phones | Kenney UI Pack, CC0 |
| Fonts | 1 display family with required TMP assets | TTF/OTF source + TMP font asset | Bold geometric arcade style; all runtime text uses TextMeshPro | Kenney Future, bundled with Kenney UI pack, CC0 |
| Merge VFX | 1 reusable particle prefab | Unity Particle System + transparent material | Firework-like radial burst with falling particles; pooled/repositioned rather than destroyed | Project-authored `MergeBurst.prefab` using imported particle textures where applicable |
| Chip motion VFX | 1 trail material | Unity `.mat`, additive/transparent | Short colored/bright trail during deal, move and hammer break-away | Project-authored |
| Unlock VFX | 1 rotating glow + 3D chip preview | UI Image + existing 3D chip model | Only shown when a new chip level unlocks; chip loops a 360° rotation | Project-authored composition; OpenGameArt sunburst sprite |
| BGM | 1 looping track | MP3, compressed/streamed as appropriate | Upbeat Las Vegas casino ambience; low mix level under SFX | “Las Vegas Casino Music” by MFCC, Pixabay Content License |
| Core chip SFX | 3 clips | OGG | Move/land, merge/collision and deal/handle; pitch variation and minimum interval prevent harsh overlap | Kenney Casino Audio, CC0 |
| Progress SFX | 1 clip | MP3 | Short collect sound, triggered when each flying star reaches the bar | LIECIO via Pixabay Content License |
| State SFX | 2 clips | MP3 | Level-up winner sting and game-over sting | PuyoPuyoMegaFan1234 and Tuomas_Data via Pixabay Content License |
| Skill SFX | 2 clips | MP3 | Poker-chip shuffle and dense falling-coin clatter for Hammer | OxidVideos and PWLPL via Pixabay Content License |

## UI screens and states

| UI | Required elements | Current implementation |
|---|---|---|
| Loading | Full-screen background, loading label, fill bar | `LoadingUI.prefab`; shown before board initialization |
| Gameplay HUD | Level badge, percentage progress bar, Deal button, Shuffle button, Hammer button, flying-star layer | `GameplayUI.prefab`; safe-area aware |
| Level Up | Level text, tap-to-continue prompt; optional unlocked 3D chip and rotating glow | `LevelUpUI.prefab`; ordinary level-up hides unlock presentation |
| Lose | Lose message and retry button | `LoseUI.prefab`; retry returns to the same level |

## Asset organization

- Runtime art: `Assets/Game/3D`, `Assets/Game/Sprite`, `Assets/Game/Sound`, `Assets/Game/VFX`.
- Reusable prefabs: `Assets/Game/Prefabs`.
- All attribution records: `Assets/Game/License`.
- Optional third-party packs that are not required by the current gameplay should be excluded from the final build where possible.

## Licensing and distribution checks

- Preserve all files under `Assets/Game/License` in the source submission.
- Include the credits listed in the root `README.md` with any public playable build.
- The tray is CC BY-SA and requires attribution/share-alike compliance.
- The imported poker-chip model and optional Coins/CasualHit/SimpleFX/LanaStudio packs do not currently have complete local license records. Verify their original store/download licenses before public distribution, or remove unused packs from the release build/source package.