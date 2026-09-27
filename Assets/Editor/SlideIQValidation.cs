using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class SlideIQValidation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tools/SlideIQ/Validate Rules")]
    public static void Validate()
    {
        for(int level=0;level<3;level++)for(int challenge=1;challenge<=1000;challenge++)
        {
            var s=SlideRules.New(level,challenge);SlideRules.Validate(s);
            Check(!SlideRules.Finished(s),"Challenge starts solved");
            Check(s.tiles.SequenceEqual(SlideRules.Initial(level,challenge)),"Unstable challenge");
            var before=(int[])s.tiles.Clone();int blank=Array.IndexOf(s.tiles,0);
            Check(!SlideRules.Move(s,blank)&&s.moves==0,"Empty tile moved");
            int index=Enumerable.Range(0,s.tiles.Length).First(i=>SlideRules.CanMove(s,i));
            Check(SlideRules.Move(s,index)&&s.moves==1,"Legal move failed");SlideRules.Validate(s);
            Check(SlideRules.Move(s,blank)&&s.tiles.SequenceEqual(before),"Move reversal failed");
            SlideRules.Validate(JsonUtility.FromJson<SlideProgress>(JsonUtility.ToJson(s)));
            s.tiles=SlideRules.Goal(SlideRules.Size(level));Check(SlideRules.Won(s),"Win not detected");
            s.revealed=true;Check(!SlideRules.Won(s)&&SlideRules.Finished(s),"Reveal counted as win");
            s.revealed=false;int swap=s.tiles[0];s.tiles[0]=s.tiles[1];s.tiles[1]=swap;
            bool rejected=false;try{SlideRules.Validate(s);}catch(ArgumentException){rejected=true;}
            Check(rejected,"Unsolvable save accepted");
        }
        Debug.Log("SlideIQ: 3000 challenges passed solvability, deterministic generation, moves, save round trips, win and reveal checks.");
    }
}
