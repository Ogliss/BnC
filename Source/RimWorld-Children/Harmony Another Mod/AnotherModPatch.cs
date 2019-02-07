using System;
using System.Collections.Generic;
using Verse;
using Harmony;

namespace RimWorldChildren
{  
    public class AnotherModPatch
    {
        //public static bool Humanoid_Alien_Races_On = false;
        public static bool RJW_On = false;
        public static bool ShowHair_On = false;
        
        private static bool IsLoaded(string mod)
        {
            return LoadedModManager.RunningModsListForReading.Any(x => x.Name == mod);
        }

        public static void AnotherModPatchRun(HarmonyInstance harmony)
        {

            //if (IsLoaded("Humanoid Alien Races 2.0"))
            //{
                
            //   //Humanoid_Alien_Races_On = true;
            //    //var original = typeof(AlienRace.HarmonyPatches).GetMethod("DrawAddons");
            //    //var prefix = typeof(AnotherModPatch).GetMethod("DrawAddons_PreA");
            //    //harmony.Patch(original, new HarmonyMethod(prefix), null);
            //}
            //else
           

            //if (!(DefDatabase<HediffDef>.GetNamedSilentFail("RJW_BabyState") is null))
            //{
            //    Log.Message("[From BnC] RJW Detected");
            //    RJW_On = true;
            //    var original = typeof(PortraitsCache).GetMethod("SetDirty");
            //    var postfix = typeof(AnotherModPatch).GetMethod("RJW_Heddiff_Replace");
            //    harmony.Patch(original, null, new HarmonyMethod(postfix), null );
            //}
            //else
            //{ Log.Message("[From BnC] RJW Not Detected"); }

            if (IsLoaded("[KV] Show Hair With Hats or Hide All Hats - 1.0"))
            {
                Log.Message("[From BnC] Show Hair 1.0 Detected");
                ShowHair_On = true;
            }

            //else
            //{ Log.Message("[From BnC] Show Hair 1.0 Not Detected"); }


            // TEST
            //   HashSet<String> tstring = new HashSet<string>(DefDatabase<SupportAlienDef>.GetNamed("makeAlienChild").supportAlienRaces);

            //   foreach (string k in tstring)
            //   {
            //       Log.Message("    " + k);
            //   }
        }

        //// don't make addon for baby & toddler 
        //public static bool DrawAddons_PreA(bool portrait, Pawn pawn, Vector3 vector, Quaternion quat, Rot4 rotation)
        //{
        //    if (pawn.ageTracker.CurLifeStageIndex < AgeStage.Child)
        //    { return false; }
        //    return true;
        //}

        // replace RJW heddiff -> CnP heddiff
        //public static void RJW_Heddiff_Replace(Pawn pawn)
        //{
        //    if (pawn.Spawned && pawn.health.hediffSet.HasHediff(HediffDef.Named("RJW_BabyState")))
        //    {
        //        pawn.health.AddHediff(HediffDef.Named("BabyState"), null, null);
        //        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("RJW_BabyState")));
                                
        //        Hediff_Baby babystate = (Hediff_Baby)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("BabyState"));
        //        for (int i = 0; i != pawn.ageTracker.CurLifeStageIndex + 1; i++)
        //        { babystate.GrowUpTo(i, true); }

        //        Pawn mother = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, x => x.gender == Gender.Female);
        //        if (mother != null)
        //        { mother.health.AddHediff(HediffDef.Named("Lactating"), ChildrenUtility.GetPawnBodyPart(pawn, "Chest"), null); }

        //        if (pawn.health.hediffSet.HasHediff(HediffDef.Named("RJW_NoManipulationFlag")))
        //        {
        //            pawn.health.AddHediff(HediffDef.Named("NoManipulationFlag"), null, null);
        //            pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("RJW_NoManipulationFlag")));                    
        //        }
        //    }           
        //}

    }
        
}
