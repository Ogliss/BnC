using RimWorld;
using Verse;
using Harmony;
using System.Reflection;


namespace RimWorldChildren
{
    // give mother Lacrating hediff
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "AddDirectRelation")]
    static class Give_Hediff_Lacrating
    {
        private static FieldInfo PawnFI = typeof(Pawn_RelationsTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
        static void AddDirectRelation_GiveLacrating(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (def == PawnRelationDefOf.Parent )
            {
                Pawn pawn = (Pawn)PawnFI.GetValue(__instance);
                Pawn mother = pawn.GetMother();                
                //Log.Message("mother : " + pawn.LabelIndefinite());
                if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Baby && mother == otherPawn)
                {                                    
                    if (mother.RaceProps.Humanlike && !mother.health.hediffSet.HasHediff(HediffDef.Named("Lactating")))
                    {
                        if (AnotherModCheck.RJW_On)
                        {
                            mother.health.AddHediff(HediffDef.Named("Lactating"), ChildrenUtility.GetPawnBodyPart(pawn, "Chest"), null);
                        }
                        else
                        {
                            mother.health.AddHediff(HediffDef.Named("Lactating"), ChildrenUtility.GetPawnBodyPart(pawn, "Torso"), null);
                        }
                    }
                }
            }    
        }
    }
}