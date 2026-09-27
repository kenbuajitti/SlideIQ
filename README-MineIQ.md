# MineIQ — Initial release

## Open and play
1. Extract this ZIP into a new folder, then add that folder in Unity Hub.
2. Open with Unity 6000.5.7f1 (the version recorded by the supplied project).
3. Open Assets/Scenes/MineIQMenuScene.unity and press Play. These dedicated scenes use a new, independent MineIQ controller identifier.
4. Run Tools > MineIQ > Validate Starter Puzzles and Rules for the included Unity-side checks.
5. Build a fresh WebGL version, with MineIQMenuScene first and MineIQGameScene second. The old Sudoku WebGL build is not included.

Use the extracted project as a complete project, rather than merging its Assets over an old clone: obsolete Sudoku scripts have been removed.

## Included
- Classic Minesweeper and Survey mode with fixed row/column mine totals.
- Beginner: 6 x 6, 6 mines. Intermediate: 8 x 8, 12 mines. Advanced: 10 x 10, 22 mines.
- 60 deterministic offline puzzles: ten per mode/difficulty, each verified solvable using visible clues without guessing.
- Safe area automatically opened at the start of every puzzle, followed by normal mine risk. The first additional player click is not guaranteed safe.
- OPEN/FLAG toggle for touch and mouse; right-click also toggles flags. F = flag, * = exposed mine, X = incorrect flag after reveal/loss.
- Empty-area flood opening, timer, previous/next puzzle, completion, reveal and reset.
- Progress preserved while switching puzzles in the current session. Export/Import restores progress and puzzle packs. Export before closing or refreshing. No account or persistent server storage.
- Responsive portrait/landscape menus, matching new backgrounds, music toggle and All IQ Games link.
- Four standalone PNG covers in Covers: Landscape, Portrait, Square and Itch-Cover. Titles are fully inside the frame. The itch cover is under 3,000,000 bytes.

## Generate more puzzles
Python 3 (standard library only):

    python Tools/generate_mine_puzzles.py --output extra-puzzles.json --seed 12345 --per-group 5

Give add-in packs distinct IDs before importing alongside the starter pack (IDs identify saved progress). Import rejects conflicting IDs. A session supports at most 100 puzzles. Old Sudoku saves are not compatible.

Verify a pack:

    python Tools/generate_mine_puzzles.py --verify Assets/Resources/mineiq-puzzles.json

The JSON format uses schemaVersion 1 and a puzzles array. Each puzzle has id, variant (classic/survey), difficulty, size, start (zero-based row-major index), mines (row-major string: 0 safe, 1 mine), and technique. The start tile and its neighbours must be mine-free.

Difficulty tiers currently describe board size and density, not a calibrated human difficulty score. Imported third-party packs receive format/safe-start validation; run the offline verifier to check their no-guess solvability. Explanatory hints and zones are future features, not part of this initial release.

## Validation performed here
- All 60 boards solved by the offline logical solver without guesses.
- C# syntax parsed across all scripts.
- Both build-scene component GUID references verified.
- Cover dimensions, file sizes and visible title margins inspected.

Unity is not installed in the preparation environment. Unity compilation, Play Mode, browser file dialogs/audio, and final WebGL testing must be checked in the Editor/browser. The included Editor validation exercises the actual C# rules, save round trips, flood opening, flags, bounds, totals and completion.

## Cover production
Generated with the built-in image tool from the supplied SudokuIQ cover as a style reference: fantasy blue/violet starry skies, floating castle islands/waterfalls, thoughtful red-haired woman with glowing brain, a flagged Minesweeper board and gold-white MineIQ title. Separate prompts requested landscape, portrait, square, and an uncropped itch.io composition with generous title margins.

## Menu/controller fix
MineIQ now uses a unique script GUID and dedicated MineIQMenuScene / MineIQGameScene build scenes. This avoids the previous reused Sudoku controller identifier when overlaying a cloned project. The legacy Tsp scenes in this package also point to MineIQ. Open MineIQMenuScene explicitly after importing. Build a fresh WebGL output; old Sudoku web output cannot reflect source changes.
