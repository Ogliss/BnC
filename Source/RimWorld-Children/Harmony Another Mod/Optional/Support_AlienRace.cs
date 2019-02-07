using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using AlienRace;

namespace RimWorldChildren
{
    public static class AliensSupport
    {
        public static readonly HashSet<String> SupportAlienRaces = new HashSet<string>(DefDatabase<SupportAlienDef>.GetNamed("makeAlienChild").supportAlienRaces);
        public static List<ThingDef_AlienRace> CurrentAliensdef = new List<ThingDef_AlienRace>();
        public static  HashSet<String> CurrentAlienRaces = new HashSet<string>();
        public const float Max_lifeExpectancy = 400f;

        public static void ChangeAliensProperty()
        {
            string st = "";
            foreach (ThingDef_AlienRace ar in DefDatabase<ThingDef_AlienRace>.AllDefs)
            {
                if (SupportAlienRaces.Contains(ar.defName))
                {
                    st = st + ar.defName + ", ";
                    CurrentAliensdef.Add(ar);
                    CurrentAlienRaces.Add(ar.defName);
                    //set max lifeExpectancy = 400
                    if (ar.race.lifeExpectancy > Max_lifeExpectancy ) ar.race.lifeExpectancy = Max_lifeExpectancy;
                }
            }
                        
            if (CurrentAliensdef == null) return;
            Log.Message("[From BnC] Using alien races now : " + st);
            ChangeStageAgesCurve();

            foreach (PawnKindDef pkd in from k in DefDatabase<PawnKindDef>.AllDefs
                                        where k.RaceProps.Humanlike
                                        select k)
            {
                if ( pkd.RaceProps.lifeStageAges.Count > 3)
                {
                    if ((float) pkd.minGenerationAge > pkd.RaceProps.lifeStageAges[2].minAge + 2f)
                    {
                        pkd.minGenerationAge = (int)pkd.RaceProps.lifeStageAges[2].minAge + 2;
                        //Log.Message("genAge Change - " + pkd.defName + "  to  >>  " + pkd.minGenerationAge);
                    }

                }                
            }
        }
        
        public static void ChangeStageAgesCurve()
        {
            foreach (ThingDef_AlienRace ar in CurrentAliensdef)
            {
                // change life stage
                List<LifeStageAge> lifeStageAges = ar.race.lifeStageAges;
                lifeStageAges[1].minAge *= 0.75f;
                lifeStageAges[2].minAge *= 0.75f;

                //const float ca = 0f;
                //float Curvevalue1 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[1].minAge+ ca);
                //float Curvevalue2 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[2].minAge+ ca);
                //float Curvevalue25 = ar.race.ageGenerationCurve.Evaluate(6f);
                //float Curvevalue3 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[3].minAge+ ca);
                //float Curvevalue4 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[4].minAge + ca);
                //Log.Message("before - " + ar.defName + "lifeStageAges1 : " + (ar.race.lifeStageAges[1].minAge + ca) + " >> " + Curvevalue1);
                //Log.Message("before - " + ar.defName + "lifeStageAges2 : " + (ar.race.lifeStageAges[2].minAge + ca) + " >> " + Curvevalue2);
                //Log.Message("before - " + ar.defName + "lifeStageAges2.5 : " + "6" + " >> " + Curvevalue25);
                //Log.Message("before - " + ar.defName + "lifeStageAges3 : " + (ar.race.lifeStageAges[3].minAge + ca) + " >> " + Curvevalue3);
                //Log.Message("before - " + ar.defName + "lifeStageAges4 : " + (ar.race.lifeStageAges[4].minAge + ca) + " >> " + Curvevalue4);

                if (ar.defName != "Ratkin")
                {
                    SimpleCurve newAgeCurve = new SimpleCurve
                    {
                        {
                            new CurvePoint(ar.race.lifeStageAges[2].minAge, 0f),              //3
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[2].minAge+2f, 10f),              //5  
                            true
                        },
                        {
                            new CurvePoint((ar.race.lifeStageAges[2].minAge+ar.race.lifeStageAges[3].minAge)/2f, 40f),              //6 
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[3].minAge, 75f),          //14
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[4].minAge + 3f, 95f),            //23
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[3].minAge + 10f, 85f),           //30
                            true
                        },
                        {
                            new CurvePoint((ar.race.lifeStageAges[3].minAge+ ar.race.lifeExpectancy)/2f, 30f),            //60
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeExpectancy, 9f),             //80
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeExpectancy * 100f /80f, 0f),             //100
                            true
                        }
                    };
                    // change curve
                    ar.race.ageGenerationCurve = newAgeCurve;
                }
                //Curvevalue1 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[1].minAge + ca);
                //Curvevalue2 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[2].minAge + ca);
                //Curvevalue25 = ar.race.ageGenerationCurve.Evaluate(6f);
                //Curvevalue3 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[3].minAge + ca);
                //Curvevalue4 = ar.race.ageGenerationCurve.Evaluate(ar.race.lifeStageAges[4].minAge + ca);
                //Log.Message("after - " + ar.defName + "lifeStageAges1 : " + (ar.race.lifeStageAges[1].minAge + ca) + " >> " + Curvevalue1);
                //Log.Message("after - " + ar.defName + "lifeStageAges2 : " + (ar.race.lifeStageAges[2].minAge + ca) + " >> " + Curvevalue2);
                //Log.Message("after - " + ar.defName + "lifeStageAges2.5 : " + "6" + " >> " + Curvevalue25);
                //Log.Message("after - " + ar.defName + "lifeStageAges3 : " + (ar.race.lifeStageAges[3].minAge + ca) + " >> " + Curvevalue3);
                //Log.Message("after - " + ar.defName + "lifeStageAges4 : " + (ar.race.lifeStageAges[4].minAge + ca) + " >> " + Curvevalue4);
            }
        }                     
    }
}
