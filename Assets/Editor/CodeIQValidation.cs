using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CodeIQValidation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tools/CodeIQ/Validate Rules")]
    public static void Validate()
    {
        int exact,other;
        CodeRules.Score(new[]{0,0,1,2},new[]{0,1,0,0},out exact,out other);Check(exact==1&&other==2,"Duplicate feedback");
        CodeRules.Score(new[]{0,0,1,1},new[]{1,1,0,0},out exact,out other);Check(exact==0&&other==4,"Swapped feedback");
        CodeRules.Score(new[]{0,0,1,1},new[]{0,0,0,0},out exact,out other);Check(exact==2&&other==0,"Overcounted symbols");
        for(int level=0;level<3;level++)for(int n=1;n<=2000;n++)
        {
            var secret=CodeRules.Secret(level,n);Check(secret.SequenceEqual(CodeRules.Secret(level,n)),"Unstable challenge");
            Check(secret.Length==CodeRules.Slots(level)&&secret.All(x=>x>=0&&x<CodeRules.Symbols(level)),"Invalid code");
            Check(level!=0||secret.Distinct().Count()==4,"Beginner repeats");
            var s=CodeRules.New(level,n);CodeRules.Validate(s);
            s.guesses=new[]{new CodeGuess{values=secret}};Check(CodeRules.Won(s),"Win detection");
            CodeRules.Validate(JsonUtility.FromJson<CodeProgress>(JsonUtility.ToJson(s)));
            s.revealed=true;Check(!CodeRules.Won(s),"Reveal marked as win");
        }
        var bad=CodeRules.New(0,1);bad.draft=new[]{0,0,-1,-1};bool rejected=false;
        try{CodeRules.Validate(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"Accepted duplicate beginner save");
        Debug.Log("CodeIQ validation passed: feedback, 6000 deterministic challenges, save round trips and win/reveal rules.");
    }
}
