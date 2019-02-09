using System;
using RimWorld;
using Verse;
using rjw;
using Harmony;

namespace RimWorldChildren.Harmony.Optional
{
    [StaticConstructorOnStartup]
    internal static class Harmony_RJW
    {
        static Harmony_RJW()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.baby_and_children.rjw_patch");

            try
            {
                ((Action)(() =>
                {
                    if ((AccessTools.Method(
                            typeof(xxx),
                            nameof(xxx.is_human)) == null))
                    {
                        //Log.Message("[From BnC] RJW Not Detected");
                        return;
                    }

                    Log.Message("[From BnC] RimJobWorld Detected");
                    AnotherModCheck.RJW_On = true;

                    //Patch
                    harmony.Patch(
                        typeof(Hediff_SimpleBaby).GetMethod("PostMake"),                        
                        new HarmonyMethod(typeof(rjw_Patches).GetMethod(nameof(rjw_Patches.PostMake_Pre))));
                }))();
            }
            catch (TypeLoadException)
            {
            }
        }
    }

    public static class rjw_Patches
    {
        public static bool PostMake_Pre(Hediff __instance)
        {
            Hediff_SimpleBaby h = (Hediff_SimpleBaby)__instance;
            Pawn pawn = h.pawn;            
            pawn.health.AddHediff(HediffDef.Named("BabyState"), null, null);
            Hediff_Baby babystate = (Hediff_Baby)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("BabyState"));
            if (babystate != null)
            { babystate.GrowUpTo(0, true); }            
            pawn.health.AddHediff(HediffDef.Named("NoManipulationFlag"), null, null);
            return false;            
         }         
    }
}
