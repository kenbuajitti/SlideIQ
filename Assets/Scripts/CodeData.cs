using System;
using System.Linq;

[Serializable] public sealed class CodeGuess { public int[] values; }
[Serializable] public sealed class CodeProgress
{
    public string kind;
    public int schemaVersion,level,challenge;
    public CodeGuess[] guesses;
    public int[] draft;
    public float seconds;
    public bool revealed;
}
public static class CodeRules
{
    public static int Slots(int level){return level==2?5:4;}
    public static int Symbols(int level){return level==2?8:6;}
    // Explicit uint PRNG: challenge identity is stable across Editor, WebGL and .NET versions.
    static uint Next(ref uint state){unchecked{state=state*1664525u+1013904223u;return state;}}
    public static int[] Secret(int level,int challenge)
    {
        if(level<0||level>2||challenge<1||challenge>999999)throw new ArgumentException("Invalid level or challenge.");
        uint state=unchecked((uint)challenge ^ ((uint)(level+1)*2654435761u));
        var pool=Enumerable.Range(0,Symbols(level)).ToList();int[] code=new int[Slots(level)];
        for(int i=0;i<code.Length;i++)
        {
            // Use upper PRNG bits; low LCG bits form poor short symbol sequences.
            int k=(int)((Next(ref state)>>8)%(uint)pool.Count);code[i]=pool[k];if(level==0)pool.RemoveAt(k);
        }
        return code;
    }
    public static CodeProgress New(int level,int challenge)
    {
        Secret(level,challenge);
        return new CodeProgress{kind="CodeIQ-progress",schemaVersion=1,level=level,challenge=challenge,guesses=new CodeGuess[0],draft=Enumerable.Repeat(-1,Slots(level)).ToArray()};
    }
    public static void Score(int[] secret,int[] guess,out int exact,out int other)
    {
        if(secret==null||guess==null||secret.Length!=guess.Length)throw new ArgumentException("Mismatched guess.");
        exact=0;other=0;int[] remaining=new int[8],wanted=new int[8];
        for(int i=0;i<secret.Length;i++)
        {
            if(secret[i]<0||secret[i]>7||guess[i]<0||guess[i]>7)throw new ArgumentException("Invalid symbol.");
            if(secret[i]==guess[i])exact++;else{remaining[secret[i]]++;wanted[guess[i]]++;}
        }
        for(int i=0;i<8;i++)other+=Math.Min(remaining[i],wanted[i]);
    }
    public static bool Won(CodeProgress s)
    {return !s.revealed && s.guesses.Length>0 && s.guesses[s.guesses.Length-1].values.SequenceEqual(Secret(s.level,s.challenge));}
    public static bool Finished(CodeProgress s){return s.revealed||Won(s)||s.guesses.Length>=10;}
    static void ValidateRow(int[] row,int level,bool draft)
    {
        if(row==null||row.Length!=Slots(level)||row.Any(x=>x<(draft?-1:0)||x>=Symbols(level)))throw new ArgumentException("Invalid guess symbols.");
        if(level==0 && row.Where(x=>x>=0).Distinct().Count()!=row.Count(x=>x>=0))throw new ArgumentException("Beginner guesses cannot repeat symbols.");
    }
    public static void Validate(CodeProgress s)
    {
        if(s==null||s.kind!="CodeIQ-progress"||s.schemaVersion!=1)throw new ArgumentException("This is not a CodeIQ progress file.");
        var secret=Secret(s.level,s.challenge);
        if(float.IsNaN(s.seconds)||float.IsInfinity(s.seconds)||s.seconds<0||s.seconds>315360000)throw new ArgumentException("Invalid timer.");
        if(s.guesses==null||s.guesses.Length>10)throw new ArgumentException("Invalid guess history.");
        ValidateRow(s.draft,s.level,true);
        for(int i=0;i<s.guesses.Length;i++)
        {
            if(s.guesses[i]==null)throw new ArgumentException("Missing guess.");
            ValidateRow(s.guesses[i].values,s.level,false);
            if(s.guesses[i].values.SequenceEqual(secret) && (i!=s.guesses.Length-1||s.revealed))throw new ArgumentException("Invalid history after solving.");
        }
    }
}
