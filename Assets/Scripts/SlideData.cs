using System;
using System.Linq;
using System.Collections.Generic;

[Serializable] public sealed class SlideProgress
{
    public string kind;
    public int schemaVersion,level,challenge,moves;
    public int[] tiles;
    public float seconds;
    public bool revealed;
}
public static class SlideRules
{
    public static int Size(int level) { return level+3; }
    public static int[] Goal(int n) { var a=Enumerable.Range(1,n*n).ToArray();a[a.Length-1]=0;return a; }
    static uint Next(ref uint state) { unchecked { state=state*1664525u+1013904223u;return state; } }
    static bool Adjacent(int a,int b,int n) { return Math.Abs(a/n-b/n)+Math.Abs(a%n-b%n)==1; }
    public static int[] Initial(int level,int challenge)
    {
        if(level<0||level>2||challenge<1||challenge>999999)throw new ArgumentException("Invalid level or challenge.");
        int n=Size(level),blank=n*n-1,previous=-1;var tiles=Goal(n);
        uint state=unchecked((uint)challenge^((uint)(level+1)*2654435761u));
        // Legal moves from the goal preserve solvability. Avoid immediate reversals.
        for(int step=0;step<80+level*120;step++)
        {
            var choices=new List<int>();
            for(int i=0;i<tiles.Length;i++)if(i!=previous&&Adjacent(i,blank,n))choices.Add(i);
            int next=choices[(int)((Next(ref state)>>8)%(uint)choices.Count)];
            tiles[blank]=tiles[next];tiles[next]=0;previous=blank;blank=next;
        }
        if(tiles.SequenceEqual(Goal(n))) {tiles[blank]=tiles[blank-1];tiles[blank-1]=0;}
        return tiles;
    }
    public static SlideProgress New(int level,int challenge)
    { return new SlideProgress{kind="SlideIQ-progress",schemaVersion=1,level=level,challenge=challenge,tiles=Initial(level,challenge)}; }
    public static bool Won(SlideProgress s) { return !s.revealed&&s.tiles.SequenceEqual(Goal(Size(s.level))); }
    public static bool Finished(SlideProgress s) { return s.revealed||Won(s); }
    public static bool CanMove(SlideProgress s,int index)
    { return index>=0&&index<s.tiles.Length&&Adjacent(index,Array.IndexOf(s.tiles,0),Size(s.level)); }
    public static bool Move(SlideProgress s,int index)
    {
        if(Finished(s)||!CanMove(s,index))return false;
        int blank=Array.IndexOf(s.tiles,0);s.tiles[blank]=s.tiles[index];s.tiles[index]=0;s.moves++;return true;
    }
    public static bool Solvable(int[] tiles,int n)
    {
        int inversions=0;
        for(int i=0;i<tiles.Length;i++)for(int j=i+1;j<tiles.Length;j++)if(tiles[i]!=0&&tiles[j]!=0&&tiles[i]>tiles[j])inversions++;
        return n%2==1?inversions%2==0:(inversions+n-Array.IndexOf(tiles,0)/n)%2==1;
    }
    public static void Validate(SlideProgress s)
    {
        if(s==null||s.kind!="SlideIQ-progress"||s.schemaVersion!=1)throw new ArgumentException("This is not a SlideIQ progress file.");
        if(s.level<0||s.level>2||s.challenge<1||s.challenge>999999)throw new ArgumentException("Invalid level or challenge.");
        int n=Size(s.level);
        if(s.tiles==null||s.tiles.Length!=n*n||s.tiles.Any(x=>x<0||x>=n*n)||s.tiles.Distinct().Count()!=n*n)throw new ArgumentException("The board must contain each tile exactly once.");
        if(!Solvable(s.tiles,n))throw new ArgumentException("This board cannot be solved.");
        if(s.moves<0||s.moves>100000000||float.IsNaN(s.seconds)||float.IsInfinity(s.seconds)||s.seconds<0||s.seconds>315360000)throw new ArgumentException("Invalid moves or timer.");
    }
}
