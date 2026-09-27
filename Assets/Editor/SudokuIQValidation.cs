using System;
using UnityEditor;
using UnityEngine;

public static class SudokuIQValidation
{
    [MenuItem("Tools/SudokuIQ/Validate Starter Puzzles")]
    public static void Validate()
    {
        var asset=Resources.Load<TextAsset>("sudokuiq-puzzles");
        if(asset==null) throw new Exception("Starter pack is missing.");
        var pack=JsonUtility.FromJson<SudokuPack>(asset.text);
        foreach(var puzzle in pack.puzzles) SudokuRules.Validate(puzzle);
        var board=new int[81];board[0]=5;
        if(!SudokuRules.Legal(board,40,5,"classic") || SudokuRules.Legal(board,40,5,"sudoku-x")) throw new Exception("Main diagonal validation failed.");
        board[0]=0;board[8]=7;
        if(!SudokuRules.Legal(board,40,7,"classic") || SudokuRules.Legal(board,40,7,"sudoku-x")) throw new Exception("Anti-diagonal validation failed.");
        foreach(var puzzle in pack.puzzles)
        {
            var progress=new SudokuProgress{puzzle=puzzle,values=puzzle.givens};
            SudokuRules.ValidateProgress(JsonUtility.FromJson<SudokuProgress>(JsonUtility.ToJson(progress)));
        }
        Debug.Log("SudokuIQ: validated "+pack.puzzles.Length+" unique-solution puzzles.");
    }
}
