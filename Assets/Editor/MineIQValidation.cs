using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MineIQValidation
{
    [MenuItem("Tools/MineIQ/Validate Starter Puzzles and Rules")]
    public static void Validate()
    {
        var pack=JsonUtility.FromJson<MinePack>(Resources.Load<TextAsset>("mineiq-puzzles").text);
        if(pack.puzzles.Select(p=>p.id).Distinct().Count()!=pack.puzzles.Length)throw new Exception("Duplicate IDs");
        foreach(var p in pack.puzzles)
        {
            MineRules.Validate(p);var s=MineRules.NewProgress(p);
            if(!s.opened[p.start] || s.opened.Where((v,i)=>v && p.mines[i]=='1').Any())throw new Exception("Unsafe opening");
            MineRules.ValidateProgress(JsonUtility.FromJson<MineProgress>(JsonUtility.ToJson(s)));
            if(MineRules.Neighbours(p,0).Count()!=3 || MineRules.Neighbours(p,p.size+1).Count()!=8)throw new Exception("Neighbour bounds");
            if(Enumerable.Range(0,p.size).Sum(r=>MineRules.RowTotal(p,r))!=p.mines.Count(c=>c=='1'))throw new Exception("Row totals");
            if(Enumerable.Range(0,p.size).Sum(c=>MineRules.ColumnTotal(p,c))!=p.mines.Count(c=>c=='1'))throw new Exception("Column totals");
            int closed=Array.FindIndex(s.opened,x=>!x);
            s.flags[closed]=true;MineRules.Open(s,closed);if(s.opened[closed])throw new Exception("Opened a flag");s.flags[closed]=false;
            for(int i=0;i<p.mines.Length;i++)if(p.mines[i]=='0')MineRules.Open(s,i);
            if(!MineRules.Complete(s))throw new Exception("Completion detection");
            s.revealed=true;if(MineRules.Complete(s))throw new Exception("Reveal counted as a win");
        }
        Debug.Log("MineIQ: validated "+pack.puzzles.Length+" boards, bounds, totals, flags, save round trips and completion. Offline generator verifies logical solvability.");
    }
}
