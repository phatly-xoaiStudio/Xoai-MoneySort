# Flick Sort — Process Plan

## Scope

Flick Sort is a portrait mobile prototype based on the challenge's Option 3. The goal was to reproduce the readable tap-to-move sorting loop of the Money Sort reference while giving it a brighter poker-chip presentation and strong audiovisual feedback.

The playable scope is intentionally small:

- Open directly into gameplay; no tutorial or main menu.
- Tap a source stack, then tap a compatible destination stack.
- Move the contiguous top group that matches the selected chip color; partial moves are allowed when the destination has fewer free slots.
- Deal new chips, merge ten matching chips into the next chip level, and score one point per consumed chip.
- Progress through increasingly demanding levels without resetting the board on level-up.
- Unlock additional chip levels and stack slots over time.
- Lose only when all available stack slots are full; retry the same level.
- Two usable skills: Shuffle and Hammer.

## Milestones

1. **Reference and rules** — Studied the reference gameplay, isolated the core interaction, defined stack capacity, partial transfer, merge, deal, score, level-up, loss, and maximum chip-level rules.
2. **Core prototype** — Built the stack model, tap selection, legal move validation, partial group transfer, randomized deal, merge resolution, scoring, level progression, and loss/retry flow.
3. **Presentation pass** — Replaced placeholder geometry with poker-chip and tray models, created ten chip colors, added the wooden tray material and green felt background, and composed portrait gameplay UI.
4. **Juice pass** — Added DOTween jump/arc movement, merge punch, hammer break-away, reusable merge particles, progress-star animation, UI transitions, button press animation, camera shake, trails, and layered sound feedback.
5. **Feature pass** — Added locked stacks, chip unlock presentation, Shuffle and Hammer skills, responsive UI/safe area, and aspect-ratio-aware camera framing.
6. **Polish and validation** — Added object pooling, reduced gameplay allocations, optimized mobile lighting/render settings, disabled unnecessary chip shadows/probes, and ran Unity compile, EditMode tests, and Play Mode checks.

## What was cut and why

- **Tutorial:** explicitly excluded; the interaction is designed to be understood immediately.
- **Save/progression persistence:** removed from the current submission scope to avoid introducing unverified state migration and retry edge cases.
- **Third skill / arbitrary chip-group pickup:** cut after testing because the chips are visually small on mobile and selecting a specific group inside a stack was difficult and unreliable. This interaction risk was discovered late, so the feature was removed rather than shipping an unclear or frustrating control.
- **Separate win popup:** excluded to match the reference flow; level-up is acknowledged by tapping the level-up UI and play continues on the same board.
- **Bonus “satisfy” showcase level:** not included. Time was invested in the core loop, responsiveness, sound, VFX, and mobile performance instead.

## Polish budget

Most polish time was spent where it directly reinforces player actions:

- Chip movement uses arced jumps rather than linear translation.
- Each landing, merge, progress-star impact, deal, level-up, loss, and skill has dedicated sound treatment.
- Merge feedback combines motion, scale punch, particles, and camera shake.
- Progress is shown as a percentage with ten sequential stars flying into the bar.
- Newly unlocked chips receive a dedicated rotating 3D presentation and glow; ordinary level-ups remain simpler.
- UI scales from a 1080 × 1920 portrait reference, respects the safe area, and the world camera adapts to narrow/tall displays without changing tray coordinates.

## Current completion status

- Unity source project: complete for review.
- Core gameplay and two skills: implemented.
- UI, sound, VFX, responsive layout, and mobile optimization: implemented.
- Automated model tests: 12 passing in the latest recorded EditMode run.
