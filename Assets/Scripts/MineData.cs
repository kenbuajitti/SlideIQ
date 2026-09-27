using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

[Serializable] public class MinePuzzle
{
    public string id, variant="classic", difficulty, mines, technique;
    public int size, start;
}
[Serializable] public class MinePack { public int schemaVersion=1; public MinePuzzle[] puzzles; }
[Serializable] public class MineProgress
{
    public MinePuzzle puzzle;
    public bool[] opened, flags;
    public float seconds;
    public bool lost, revealed;
}
[Serializable] public class MineSave
{
    public int schemaVersion=1;
    public string kind="MineIQ-progress", activePuzzleId;
    public MineProgress[] progress;
}
public static class MineRules
{
    public static IEnumerable<int> Neighbours(MinePuzzle p,int i)
    {
        int x=i%p.size,y=i/p.size;
        for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
            if((dx!=0 || dy!=0) && x+dx>=0 && x+dx<p.size && y+dy>=0 && y+dy<p.size)
                yield return (y+dy)*p.size+x+dx;
    }
    public static int Adjacent(MinePuzzle p,int i) { return Neighbours(p,i).Count(j=>p.mines[j]=='1'); }
    public static int RowTotal(MinePuzzle p,int row) { int n=0;for(int x=0;x<p.size;x++)if(p.mines[row*p.size+x]=='1')n++;return n; }
    public static int ColumnTotal(MinePuzzle p,int col) { int n=0;for(int y=0;y<p.size;y++)if(p.mines[y*p.size+col]=='1')n++;return n; }
    public static MineProgress NewProgress(MinePuzzle p)
    {
        var s=new MineProgress{puzzle=p,opened=new bool[p.size*p.size],flags=new bool[p.size*p.size]};Open(s,p.start);return s;
    }
    public static void Open(MineProgress s,int i)
    {
        var todo=new Queue<int>();todo.Enqueue(i);
        while(todo.Count>0)
        {
            int j=todo.Dequeue();
            if(s.opened[j] || s.flags[j] || s.puzzle.mines[j]=='1')continue;
            s.opened[j]=true;
            if(Adjacent(s.puzzle,j)==0)foreach(int k in Neighbours(s.puzzle,j))todo.Enqueue(k);
        }
    }
    public static bool Complete(MineProgress s)
    {
        if(s.lost || s.revealed)return false;
        for(int i=0;i<s.opened.Length;i++)if(s.puzzle.mines[i]=='0' && !s.opened[i])return false;
        return true;
    }
    public static void Validate(MinePuzzle p)
    {
        if(p==null || string.IsNullOrEmpty(p.id) || p.id.Length>100)throw new ArgumentException("Missing or invalid puzzle ID.");
        if(p.size<5 || p.size>12 || p.mines==null || p.mines.Length!=p.size*p.size || p.mines.Any(c=>c!='0' && c!='1'))throw new ArgumentException("Minefields must be 5–12 square grids of 0 and 1.");
        if(p.variant!="classic" && p.variant!="survey")throw new ArgumentException("Unknown mode.");
        if(p.difficulty!="Beginner" && p.difficulty!="Intermediate" && p.difficulty!="Advanced")throw new ArgumentException("Unknown difficulty.");
        if(p.start<0 || p.start>=p.mines.Length || p.mines[p.start]=='1' || Adjacent(p,p.start)!=0)throw new ArgumentException("The opening must be an empty safe tile.");
        int mines=p.mines.Count(c=>c=='1');if(mines<1 || mines>=p.mines.Length-1)throw new ArgumentException("Invalid mine count.");
    }
    public static void ValidateProgress(MineProgress s)
    {
        if(s==null)throw new ArgumentException("Missing progress.");Validate(s.puzzle);int n=s.puzzle.mines.Length;
        if(s.opened==null || s.flags==null || s.opened.Length!=n || s.flags.Length!=n || float.IsNaN(s.seconds) || float.IsInfinity(s.seconds) || s.seconds<0 || s.seconds>315360000)throw new ArgumentException("Invalid progress values.");
        bool hit=false;
        for(int i=0;i<n;i++)
        {
            if(s.opened[i] && s.flags[i])throw new ArgumentException("An open tile cannot be flagged.");
            if(s.opened[i] && s.puzzle.mines[i]=='1')hit=true;
        }
        if(hit!=s.lost || !s.opened[s.puzzle.start])throw new ArgumentException("Inconsistent puzzle progress.");
    }
}
