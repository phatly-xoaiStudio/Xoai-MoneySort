# Flick Sort - Asset Inventory

This file documents the assets currently stored under `Assets/Game`, their role in the Money Sort / Flick Sort prototype, and the license files bundled with the project.

## Folder structure

| Folder | Contents |
|---|---|
| `3D/Models/PokerChipGameplay` | Main poker-chip and chip-tray FBX models used by the prototype. |
| `3D/Materials` | Five URP Lit chip-color materials: red, blue, yellow, purple, and green. |
| `3D/Textures/PolyHaven_WoodenPlanks` | 1K diffuse and OpenGL normal maps used by the wooden chip tray. |
| `3D/Source/PokerChipsSource` | Original/source poker-chip FBX retained for reference. |
| `3D/ThirdParty/Coins` | Optional coin, banknote, gem, and chest model pack. Not required by the poker-chip theme. |
| `UI/KenneyUIPack` | Kenney UI sprites and Kenney Future fonts. |
| `Sound/kenney_casino-audio` | Casino-themed audio clips. |
| `Sound/kenney_impact-sounds` | General impact and feedback audio clips. |
| `VFX/FlickSort` | Project-specific trail material. |
| `VFX/ThirdParty/CasualHit` | Optional hit particle prefabs. |
| `VFX/ThirdParty/SimpleFX` | Optional general-purpose particle prefabs. |
| `VFX/ThirdParty/LanaStudio` | Optional Hyper Casual FX particle pack. |
| `Scripts/Runtime` | Runtime scripts, currently including `WinPopupView`. |
| `Scripts/Editor` | Editor-only asset setup tool. It is not included in player builds. |
| `Prefabs/Gameplay` | Main reusable `Chip` and `ChipTray` prefabs. |
| `Prefabs/UI` | Reusable `WinPopup` prefab. |
| `Prefabs/VFX` | Reusable `BlobShadow` prefab. |
| `License` | All license/attribution text files found in the imported game assets. |

## Assets currently used or prepared for gameplay

- `Chip.prefab`: main sortable poker chip prepared for gameplay. It is not currently referenced as a prefab instance by `SampleScene`.
- `ChipTray.prefab`: tray/container model. `SampleScene` currently references this prefab.
- `WoodMat`: tray material using the Poly Haven Wooden Planks diffuse and normal textures.
- `Chip_Red`, `Chip_Blue`, `Chip_Yellow`, `Chip_Purple`, `Chip_Green`: URP materials prepared for chip categories.
- `Trail_Additive`: lightweight transparent trail material prepared for flick feedback.
- `BlobShadow.prefab`: low-cost circular shadow prepared for chips.
- `WinPopup.prefab`: completion popup using the Kenney UI pack.
- Kenney casino and impact audio: available, but no gameplay scripts currently trigger these clips.
- Third-party VFX packs: available, but no effect prefab is currently wired into the main gameplay scene.
- Coins pack: optional content only; it is not needed for the selected poker-chip presentation.

## License and attribution

All bundled license text files are centralized in `Assets/Game/License`.

| Asset | License information found in project | Local document |
|---|---|---|
| Kenney UI Pack | Creative Commons Zero (CC0 1.0). Credit is optional. | `License/LicenseKenny.txt` |
| Kenney Casino Audio | Creative Commons Zero (CC0 1.0). Credit is optional. | `License/Kenney_Casino_Audio_CC0.txt` |
| Kenney Impact Sounds | Creative Commons Zero (CC0 1.0). Credit is optional. | `License/Kenney_Impact_Sounds_CC0.txt` |
| Poker chip tray by RandomRust | Creative Commons Attribution-ShareAlike. Attribution and share-alike obligations apply. | `License/LICENSEPokerChipTray.txt` |
| Poly Haven Wooden Planks | Creative Commons Zero (CC0 1.0). Charlotte Baglioni (photography), Dario Barresi (processing). | `License/PolyHaven_WoodenPlanks_CC0.txt` |

No separate license text file was found inside the imported poker-chip model, Coins, Casual Hit, SimpleFX, or Lana Studio folders. Keep the original store/download receipts and license terms for those assets before distribution. Assets obtained through the Unity Asset Store are generally subject to the Unity Asset Store EULA, but eligibility and redistribution rights must be checked against each asset's original listing.

## Suggested credits

```text
UI and audio assets by Kenney (kenney.nl), licensed CC0.
Poker chip tray by RandomRust, licensed CC BY-SA.
Wooden Planks texture by Charlotte Baglioni and Dario Barresi via Poly Haven, licensed CC0.
```

## Notes

- Unity references are GUID-based. Asset files and their `.meta` files were moved together so existing prefab and scene references remain intact.
- The main scene remains at `Assets/Scenes/SampleScene.unity`; project settings, packages, plugins, and Unity template files are intentionally kept outside `Assets/Game`.
- Before publishing, verify the original license pages for every third-party pack that has no bundled license document.
