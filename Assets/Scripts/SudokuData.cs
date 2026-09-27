using System;
using System.Collections.Generic;

[Serializable] public class SudokuPuzzle
{
    public string id, variant = "classic", difficulty, givens, solution;
    public string technique;
}
[Serializable] public class SudokuPack
{
    public int schemaVersion = 1;
    public SudokuPuzzle[] puzzles;
}
[Serializable] public class SudokuProgress
{
    public SudokuPuzzle puzzle;
    public string values;
    public int[] notes = new int[81];
    public float seconds;
    public bool revealed;
}
[Serializable] public class SudokuSave
{
    public int schemaVersion = 1;
    public string kind = "SudokuIQ-progress";
    public string activePuzzleId;
    public SudokuProgress[] progress;
}
public static class SudokuRules
{
    public static int[] Parse(string text)
    {
        if (text == null || text.Length != 81) throw new ArgumentException("A grid must contain exactly 81 digits.");
        var a = new int[81];
        for (int i = 0; i < 81; i++)
        {
            if (text[i] < '0' || text[i] > '9') throw new ArgumentException("Grids use digits 0–9, with 0 for empty cells.");
            a[i] = text[i] - '0';
        }
        return a;
    }
    public static string Encode(int[] a) { return string.Concat(Array.ConvertAll(a, n => n.ToString())); }
    public static bool OnDiagonal(int i) { return i / 9 == i % 9 || i / 9 + i % 9 == 8; }
    public static bool Peer(int a, int b, string variant = "classic")
    {
        return a / 9 == b / 9 || a % 9 == b % 9 || (a / 27 == b / 27 && a % 9 / 3 == b % 9 / 3)
            || (variant == "sudoku-x" && ((a / 9 == a % 9 && b / 9 == b % 9)
                || (a / 9 + a % 9 == 8 && b / 9 + b % 9 == 8)));
    }
    public static bool Legal(int[] a, int index, int number, string variant = "classic")
    {
        if (number < 1 || number > 9) return false;
        for (int j = 0; j < 81; j++) if (j != index && a[j] == number && Peer(index, j, variant)) return false;
        return true;
    }
    public static bool Consistent(int[] a, string variant = "classic")
    {
        for (int i = 0; i < 81; i++) if (a[i] != 0 && !Legal(a, i, a[i], variant)) return false;
        return true;
    }
    public static bool Complete(int[] a, string variant = "classic") { return Array.IndexOf(a, 0) < 0 && Consistent(a, variant); }
    static int Count(int[] a, ref int budget, string variant)
    {
        if (--budget < 0) throw new ArgumentException("Puzzle is too complex to validate safely.");
        int index = -1, best = 10, mask = 0;
        for (int i = 0; i < 81; i++)
        {
            if (a[i] != 0) continue;
            int bits = 0, count = 0;
            for (int n = 1; n <= 9; n++) if (Legal(a, i, n, variant)) { bits |= 1 << n; count++; }
            if (count == 0) return 0;
            if (count < best) { index = i; best = count; mask = bits; if (best == 1) break; }
        }
        if (index < 0) return 1;
        int total = 0;
        for (int n = 1; n <= 9; n++) if ((mask & (1 << n)) != 0)
        {
            a[index] = n; total += Count(a, ref budget, variant); a[index] = 0;
            if (total >= 2) return 2;
        }
        return total;
    }
    public static void Validate(SudokuPuzzle p)
    {
        if (p == null || string.IsNullOrWhiteSpace(p.id) || p.id.Length > 80) throw new ArgumentException("Missing or invalid puzzle ID.");
        if (p.variant != "classic" && p.variant != "sudoku-x") throw new ArgumentException("Unknown Sudoku variant.");
        if (p.difficulty != "Beginner" && p.difficulty != "Intermediate" && p.difficulty != "Advanced") throw new ArgumentException("Unknown difficulty.");
        int[] g = Parse(p.givens), s = Parse(p.solution);
        if (!Consistent(g, p.variant) || !Complete(s, p.variant)) throw new ArgumentException("Invalid givens or solution.");
        int clues = 0;
        for (int i = 0; i < 81; i++) if (g[i] != 0) { clues++; if (g[i] != s[i]) throw new ArgumentException("Solution does not match givens."); }
        if (p.variant == "classic" && clues < 17) throw new ArgumentException("Classic puzzles require at least 17 clues.");
        int budget = 100000;
        if (Count((int[])g.Clone(), ref budget, p.variant) != 1) throw new ArgumentException("Every puzzle must have exactly one solution.");
    }
    public static void ValidateProgress(SudokuProgress p)
    {
        if (p == null) throw new ArgumentException("Missing progress record.");
        Validate(p.puzzle);
        int[] v = Parse(p.values), g = Parse(p.puzzle.givens);
        if (!Consistent(v, p.puzzle.variant)) throw new ArgumentException("Saved numbers conflict with Sudoku rules.");
        if (p.notes == null || p.notes.Length != 81) throw new ArgumentException("Invalid pencil marks.");
        if (float.IsNaN(p.seconds) || float.IsInfinity(p.seconds) || p.seconds < 0 || p.seconds > 315360000) throw new ArgumentException("Invalid timer.");
        for (int i = 0; i < 81; i++)
        {
            if (g[i] != 0 && g[i] != v[i]) throw new ArgumentException("Saved progress changes a given.");
            if (p.notes[i] < 0 || (p.notes[i] & ~1022) != 0 || (v[i] != 0 && p.notes[i] != 0)) throw new ArgumentException("Invalid pencil marks.");
            for (int n = 1; n <= 9; n++) if ((p.notes[i] & (1 << n)) != 0 && !Legal(v, i, n, p.puzzle.variant)) throw new ArgumentException("A pencil mark conflicts with the board.");
        }
    }
}
