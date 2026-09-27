# SudokuIQ — responsive cover and Sudoku-X update

## Open and play

1. Extract this ZIP into a new folder. It is a complete source project; do not copy it into an existing Assets folder.
2. Add the extracted SudokuIQ-main folder in Unity Hub. Use Unity **6000.5.7f1**, matching your uploaded project.
3. Open **Assets/Scenes/TspMenuScene.unity**, then press Play.
4. The Sudoku menu and board are created at runtime. The edit-mode scene is intentionally minimal.

The original scene filenames and GUIDs are retained so the existing build list works. TspGameScene can also be opened directly to test gameplay. The included source ZIP omits old compiled WordIQ builds, Android binaries, recordings and temporary build folders. Original WordIQ art and unused legacy scripts are retained but are not used by the new scenes.

## Included

- Classic 9×9 Sudoku and Sudoku-X, numbers 1–9. In Sudoku-X both corner-to-corner diagonals must also contain 1–9 without repeats. The diagonal cells are tinted on both the play and Reveal boards.
- Twelve starter puzzles: two each for Beginner, Intermediate and Advanced in EACH variant. The original six Classic puzzles and their IDs are unchanged.
- Number-first input. Choose a digit and click a white cell; illegal destinations are grey and disabled.
- Fixed givens, erasable player entries, pencil marks, and Undo.
- Conflicting peer pencil marks are removed automatically when a number is placed.
- Reveal displays a separate completed board, preserves player entries and marks the puzzle as revealed.
- Difficulty buttons, previous/next puzzle, timer, completion recognition and session progress for each puzzle.
- Separate landscape/portrait cover artwork, fitted without cropping on the menu. White description and black text on white menu buttons; title appears only in the artwork. Gameplay remains responsive with music and speaker toggle.
- ALL IQ GAMES below the menu buttons opens https://playiqgames.itch.io/ in a new browser tab, leaving the game open.
- CoverArt/SudokuIQ-Itch-Cover.png is a separate itch.io cover with generous title margins.
- JSON progress export/import and add-in puzzle pack import. No account or server is required.

A legal placement means it does not repeat a number in its row, column or box. In Sudoku-X this also checks both main diagonals. It may still lead to a dead end. The game deliberately does not use the stored solution to reject otherwise legal moves.

## Saving progress

Progress is held in memory only. **Export before closing, refreshing, or stopping Play Mode.** Switching puzzles or visiting the menu keeps progress within the same session.

EXPORT downloads one JSON file in WebGL. In the Unity Editor it opens a Save dialog. The file includes all loaded puzzle definitions, entries, pencil marks, timers, the active puzzle and reveal flags. IMPORT restores that file after validation and asks before replacing the current session. Undo history is not exported; Undo is for moves made since the current puzzle was selected. Time counts active play, not days spent away.

Add-in packs merge into the current session without replacing progress. Duplicate IDs with identical puzzle definitions are skipped; conflicting IDs are rejected. Export after importing a pack to retain it for the next session. Limits: 100 puzzles per session and 2 MB per import file. The variant field accepts classic or sudoku-x. Old Classic save files remain supported; restoring one also keeps newly bundled Sudoku-X puzzles available.

File import/export is implemented for **WebGL and Unity Editor**. Standalone desktop builds export to Application.persistentDataPath but do not yet have a native import dialog.

## Offline generation

Install Python 3.10 or later on your development computer. Players do not need Python.

From the project folder, regenerate the starter set:

```bat
py Tools\generate_puzzles.py
```

Generate an additional pack without changing the bundled starter set:

```bat
py Tools\generate_puzzles.py --each 2 --seed 1002 --prefix pack02 --output SudokuIQ-pack02.json
```

Use a new prefix for each new pack so IDs do not collide. `--each` is the number per difficulty per variant (1–30 for one variant, 1–16 for both). Import the generated JSON using IMPORT. For a bundled release, the game loads Assets/Resources/sudokuiq-puzzles.json.

Use `--variant classic`, `--variant sudoku-x`, or `--variant both` (the default). Both generates two sets; the second set gets an -x ID prefix suffix and uses seed + 1. A standalone Sudoku-X pack can be generated with:

```bat
py Tools\generate_puzzles.py --variant sudoku-x --each 2 --seed 1020 --prefix extra-x --output SudokuIQ-X-pack.json
```

The generator fills a board, removes clues while checking that exactly one solution remains, then uses a deterministic logical solver to rate the puzzle:

- Beginner: naked singles.
- Intermediate: hidden singles are also required by this solver.
- Advanced: locked candidates and/or naked pairs are also required by this solver.

Puzzles requiring guessing or unsupported solving techniques are excluded. These are practical technique-based ratings, not a claim that every solver will experience the same difficulty. Clue count alone does not set the tier.

Starter-pack verification:

```bat
py Tools\verify_puzzles.py
```

This uses a separate bit-mask solver to confirm exactly one solution and the stored answer. It also checks the twelve starter puzzles' tier counts and generator ratings. Inside Unity, **Tools > SudokuIQ > Validate Starter Puzzles** runs the C# uniqueness validation used for imports.

## WebGL / itch.io

The two existing build-list scenes now start SudokuIQ. The Web - Mobile - Release profile and global settings use SudokuIQ as the product name. The inherited responsive WebGL template is retained, with decompression fallback enabled.

Build a fresh WebGL output, ZIP its contents with index.html at the ZIP root, and upload it as an HTML game. Build fresh output for this update. Test EXPORT and IMPORT from the hosted game before publishing. No deployment has been performed.

## Validation performed here

- All twelve puzzles independently checked for unique solutions matching the stored answers.
- Technique classification and two-per-tier-per-variant counts checked. Both Sudoku-X diagonal constraints checked, including rejection of conflicts that would be legal in Classic.
- C# source syntax parsed and both scene/controller references checked.
- Browser file bridge tested with mocked browser APIs: download filename/content, imported JSON callback, file size rejection and cancelled/empty selection.

The Unity Editor and an actual WebGL build were not available in this environment, so C# compilation, visual layout, pointer input and the hosted download/upload flow still need a Unity Play Mode / WebGL smoke test. Start by placing a number, toggling a note, erasing/undoing, revealing and returning, then exporting, restarting and importing. Also test a portrait Game view.
