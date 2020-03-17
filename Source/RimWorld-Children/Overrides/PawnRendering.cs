using System;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace RimWorldChildren
{
    //[enum]

    /*[HarmonyPatch(typeof(BodyType), "BodyTypeExpanded")]
    public enum BodyType_BodyTypeExpanded : byte
    {
        Undefined,
        Male,
        Female,
        Thin,
        Hulk,
        Fat,
        Toddler
    }*/

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "ApparelChanged")]
    public static class Pawn_ApparelTracker_ApparelChanged_Patch {
        [HarmonyPostfix]
        internal static void ApparelChanged_Postfix(ref Pawn_ApparelTracker __instance) {
            Pawn_ApparelTracker _this = __instance;
            LongEventHandler.ExecuteWhenFinished(delegate {
                Children_Drawing.ResolveAgeGraphics(_this.pawn.Drawer.renderer.graphics);
            });
        }
    }

    [HarmonyPatch(typeof(PawnGraphicSet), "ResolveAllGraphics")]
    public static class PawnGraphicSet_ResolveAllGraphics_Patch {
        [HarmonyPostfix]
        internal static void ResolveAllGraphics_Patch(ref PawnGraphicSet __instance) {
            Pawn pawn = __instance.pawn;
            PawnGraphicSet _this = __instance;
            if (pawn.RaceProps.Humanlike) {
                Children_Drawing.ResolveAgeGraphics(__instance);
                LongEventHandler.ExecuteWhenFinished(delegate {
                    _this.ResolveApparelGraphics();
                });
            }
        }
    }

    [HarmonyPatch(typeof(PawnGraphicSet), "ResolveApparelGraphics")]
    public static class PawnGraphicSet_ResolveApparelGraphics_Patch {
        [HarmonyPrefix]
        internal static void ResolveApparelGraphics_Patch(ref PawnGraphicSet __instance) {
            Pawn pawn = __instance.pawn;
            // Updates the beard
            if (pawn.apparel != null && pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.UpperHead) && pawn.RaceProps.Humanlike) {
                Children_Drawing.ResolveAgeGraphics(__instance);
            }
        }
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ResolveApparelGraphics_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> ILs = instructions.ToList();
            int injectIndex = ILs.FindIndex(ILs.FindIndex(x => x.opcode == OpCodes.Ldloca_S) + 1, x => x.opcode == OpCodes.Ldloca_S) - 2; // Second occurence
            ILs.RemoveRange(injectIndex, 2);
            MethodInfo childBodyCheck = typeof(Children_Drawing).GetMethod("ModifyChildBodyType");
            ILs.Insert(injectIndex, new CodeInstruction(OpCodes.Call, childBodyCheck));

            foreach (CodeInstruction IL in ILs) {
                yield return IL;
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new[] { typeof(Vector3), typeof(float), typeof(Boolean), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(Boolean), typeof(Boolean), typeof(Boolean) })]
    [HarmonyBefore(new string[] { "rimworld.erdelf.alien_race.main" })]

    public static class PawnRenderer_RenderPawnInternal_Patch {

        // ShowHair Patch
        private static FieldInfo PawnFI = typeof(PawnRenderer).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyBefore(new string[] { "com.showhair.rimworld.mod" })]
        public static void Postfix(PawnRenderer __instance, ref Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait, bool headStump)
        {
            if (AnotherModCheck.ShowHair_On)
            {
                Pawn pawn = (Pawn)PawnFI.GetValue(__instance);
                rootLoc = Children_Drawing.ModifyChildYPosOffset(rootLoc, pawn, portrait);
            }
        }


        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RenderPawnInternal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILgen)
        {
            List<CodeInstruction> ILs = instructions.ToList();
            // Change the root location of the child's draw position
            int injectIndex0 = ILs.FindIndex(x => x.opcode == OpCodes.Ldarg_1) + 1;
            List<CodeInstruction> injection0 = new List<CodeInstruction> {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
                new CodeInstruction(OpCodes.Ldarg_S, 7), //portrait
				new CodeInstruction(OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyChildYPosOffset")),
            };

            // We actually have to change this in a lot of places, so we repeat the same injection
            ILs.InsertRange(injectIndex0, injection0);
            foreach (int i in new List<int> { 6, 7, 8, 12, 31 }) {
                ILs.InsertRange(ILs.FindIndex(x => x.opcode == OpCodes.Stloc_S && x.operand as LocalBuilder != null && ((LocalBuilder)x.operand).LocalIndex == i), injection0);
            }

            // Skip past the head drawing code if the pawn is a human toddler or younger
            int injectIndex1 = ILs.FindIndex(x => x.opcode == OpCodes.Ldarg_3);
            Label babyDrawBodyJump = ILgen.DefineLabel();
            ILs[injectIndex1 + 2].labels = new List<Label> { babyDrawBodyJump };
            List<CodeInstruction> injection1 = new List<CodeInstruction> {
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn")),
                new CodeInstruction (OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "ageTracker")),
                new CodeInstruction (OpCodes.Call, typeof(Pawn_AgeTracker).GetProperty("CurLifeStageIndex").GetGetMethod()),
                new CodeInstruction (OpCodes.Ldc_I4_2),
                new CodeInstruction (OpCodes.Blt, babyDrawBodyJump),
            };
            ILs.InsertRange(injectIndex1, injection1);

            // Ensure pawn is a child or higher before drawing head
            int injectIndex2 = ILs.FindIndex(x => x.opcode == OpCodes.Ldfld && x.operand == AccessTools.Field(typeof(PawnGraphicSet), "headGraphic")) + 2;
            Label notHumanJump = ILgen.DefineLabel();
            List<CodeInstruction> injection2 = new List<CodeInstruction> {
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
                new CodeInstruction (OpCodes.Call, typeof(ChildrenUtility).GetMethod("RaceUsesChildren")),
                new CodeInstruction (OpCodes.Brfalse, notHumanJump),
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
                new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod("EnsurePawnIsChildOrOlder")),
                new CodeInstruction (OpCodes.Brfalse, ILs [injectIndex2 - 1].operand),
                new CodeInstruction (OpCodes.Nop){labels = new List<Label>{notHumanJump}},
            };
            ILs.InsertRange(injectIndex2, injection2);

            // Modify the scale of a hat graphic when worn by a child
            //int injectIndex3 = ILs.FindIndex (x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 18) + 1;

            int injectIndex3 = ILs.FindIndex(x => x.opcode == OpCodes.Call && x.operand == typeof(GenDraw).GetMethod("DrawMeshNowOrLater", AccessTools.all)) + 4;
            List<CodeInstruction> injection3 = new List<CodeInstruction> {
                new CodeInstruction (OpCodes.Ldloc_S, 19),
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
                new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod("ModifyHatForChild")),
                new CodeInstruction (OpCodes.Stloc_S, 19),
            };
            ILs.InsertRange(injectIndex3, injection3);

            // Modify the scale of a hair graphic when drawn on a child
            int injectIndex4 = ILs.FindIndex(x => x.opcode == OpCodes.Callvirt && x.operand == AccessTools.Method(typeof(PawnGraphicSet), "HairMatAt")) + 2;
            List<CodeInstruction> injection4 = new List<CodeInstruction> {
                new CodeInstruction (OpCodes.Ldloc_S, 25),
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField("pawn", AccessTools.all)),
                new CodeInstruction (OpCodes.Call, AccessTools.Method(typeof(Children_Drawing), "ModifyHairForChild")),
                new CodeInstruction (OpCodes.Stloc_S, 25),
            };
            ILs.InsertRange(injectIndex4, injection4);

            // Modify the scale of clothing graphics when worn by a child
            int injectIndex5 = ILs.FindIndex(x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 5) + 1;
            List<CodeInstruction> injection5 = new List<CodeInstruction> {
                new CodeInstruction (OpCodes.Ldloc_S, 5),
                new CodeInstruction (OpCodes.Ldarg_0),
                new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField ("pawn", AccessTools.all)),
                new CodeInstruction (OpCodes.Ldarg_S, 4),
                new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod ("ModifyClothingForChild")),
                new CodeInstruction (OpCodes.Stloc_S, 5),
            };
            ILs.InsertRange(injectIndex5, injection5);

            // patch
            int injectIndex6 = ILs.FindIndex(x => x.opcode == OpCodes.Stloc_S && x.operand is LocalBuilder && ((LocalBuilder)x.operand).LocalIndex == 28) + 1;
            List<CodeInstruction> injection6 = new List<CodeInstruction> {
               new CodeInstruction (OpCodes.Ldloc_S, 28),
               new CodeInstruction (OpCodes.Ldarg_0),
               new CodeInstruction (OpCodes.Ldfld, typeof(PawnRenderer).GetField ("pawn", AccessTools.all)),
               new CodeInstruction (OpCodes.Ldarg_S, 4),
               new CodeInstruction (OpCodes.Call, typeof(Children_Drawing).GetMethod ("ModifyClothingForChild")),
               new CodeInstruction (OpCodes.Stloc_S, 28),
            };
            ILs.InsertRange(injectIndex6, injection6);
            // 

            foreach (CodeInstruction IL in ILs) {
                yield return IL;
            }
        }
    }
    
    public static class Children_Drawing
	{
		internal static void ResolveAgeGraphics(PawnGraphicSet graphics){
			LongEventHandler.ExecuteWhenFinished (delegate {

				//if (!graphics.pawn.RaceProps.Humanlike) {
				if (!ChildrenUtility.RaceUsesChildren(graphics.pawn)) {
					return;
				}

				// Beards
				String beard = "";
				if (graphics.pawn.story.hairDef != null) {
					if (graphics.pawn.story.hairDef.hairTags.Contains ("Beard")) {
						if (graphics.pawn.apparel.BodyPartGroupIsCovered (BodyPartGroupDefOf.UpperHead) && !graphics.pawn.story.hairDef.hairTags.Contains ("DrawUnderHat")) {
							beard = "_BeardOnly";
						}
						if (graphics.pawn.ageTracker.CurLifeStageIndex <= AgeStage.Teenager) {
							graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (DefDatabase<HairDef>.GetNamed ("Mop").texPath, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
						} else
							graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (graphics.pawn.story.hairDef.texPath + beard, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
					} else
						graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> (graphics.pawn.story.hairDef.texPath, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
				}

				// Reroute the graphics for children
				// For babies and toddlers
				if (graphics.pawn.ageTracker.CurLifeStageIndex <= AgeStage.Baby) {
					string toddler_hair = "Boyish";
					if (graphics.pawn.gender == Gender.Female) {
						toddler_hair = "Girlish";
					}
					graphics.hairGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/Children/Hairs/Child_" + toddler_hair, ShaderDatabase.Cutout, Vector2.one, graphics.pawn.story.hairColor);
					graphics.headGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/null", ShaderDatabase.Cutout, Vector2.one, Color.white);

					// The pawn is a baby
					if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Baby) {
						graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Single> ("Things/Pawn/Humanlike/Children/Bodies/Newborn", ShaderDatabase.CutoutSkin, Vector2.one, graphics.pawn.story.SkinColor);
					}
				}

				// The pawn is a toddler
				if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler) {
					string upright = "";
					if (graphics.pawn.ageTracker.AgeBiologicalYears >= 1) {
						upright = "Upright";
					}
					graphics.nakedGraphic = GraphicDatabase.Get<Graphic_Multi> ("Things/Pawn/Humanlike/Children/Bodies/Toddler" + upright, ShaderDatabase.CutoutSkin, Vector2.one, graphics.pawn.story.SkinColor);
				}
				// The pawn is a child
				else if (graphics.pawn.ageTracker.CurLifeStageIndex == AgeStage.Child) {
					graphics.nakedGraphic = Children_Drawing.GetChildBodyGraphics (graphics, ShaderDatabase.CutoutSkin, graphics.pawn.story.SkinColor);
					graphics.headGraphic = Children_Drawing.GetChildHeadGraphics (graphics, ShaderDatabase.CutoutSkin, graphics.pawn.story.SkinColor);
				}
			});
		}
        
		// My own methods
		internal static Graphic GetChildHeadGraphics(PawnGraphicSet graphicSet, Shader shader, Color skinColor)
		{
            Graphic_Multi graphic = null;
            Pawn pawn = graphicSet.pawn;
            if (ChildrenUtility.IsHumanlikeChild(pawn))
            {                
                string str = "Male_Child";
                string path = "Things/Pawn/Humanlike/Children/Heads/" + str;
                graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, skinColor) as Graphic_Multi;
            }
            else
            {
                graphic = graphicSet.headGraphic as Graphic_Multi;
            }
			return graphic;
		}
        
        internal static Graphic GetChildBodyGraphics(PawnGraphicSet graphicSet, Shader shader, Color skinColor)
		{
            Graphic_Multi graphic = null;
            Pawn pawn = graphicSet.pawn;
            if (ChildrenUtility.IsHumanlikeChild(pawn))
            {
                string str = "Naked_Boy";
                if (graphicSet.pawn.gender == Gender.Female)
                {
                    str = "Naked_Girl";
                }
                string path = "Things/Pawn/Humanlike/Children/Bodies/" + str;
                graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, skinColor) as Graphic_Multi;
            }
            else
            {
                graphic = graphicSet.nakedGraphic as Graphic_Multi;
            }
            return graphic;            
		}

		// Injected methods
		public static BodyTypeDef ModifyChildBodyType(Pawn pawn){
			if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child)
				return BodyTypeDefOf.Thin;
            //can't use harmony to patch enum so far as I can tell so I'll use preexisting
            //thin and fat to represent the crawling and upright toddler types until a better
            //system can be implemented
            if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler)
            {
                if (pawn.ageTracker.AgeBiologicalYears >= 1)
                {
                    return BodyTypeDefOf.Fat;
                }
                return BodyTypeDefOf.Thin;
            }
            
			return pawn.story.bodyType;
		}
		public static Vector3 ModifyChildYPosOffset(Vector3 pos, Pawn pawn, bool portrait){
			Vector3 newPos = pos;
			if (pawn.RaceProps != null && pawn.RaceProps.Humanlike) {
				// move the draw target down to compensate for child shortness
				if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child && !pawn.InBed()) {
					newPos.z -= 0.15f ;
				}
				if (pawn.InBed () && !portrait && pawn.CurrentBed().def.size.z == 1) {
					Building_Bed bed = pawn.CurrentBed ();
					// Babies and toddlers get drawn further down along the bed
					if (pawn.ageTracker.CurLifeStageIndex < AgeStage.Child) {
						Vector3 vector = new Vector3 (0, 0, 0.5f).RotatedBy (bed.Rotation.AsAngle);
						newPos -= vector;
					// ... as do children, but to a lesser extent
					} else if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child) { // Are we in a crib?
						Vector3 vector = new Vector3 (0, 0, 0.2f).RotatedBy (bed.Rotation.AsAngle);
						newPos -= vector;
					}
					newPos += new Vector3 (0, 0, 0.2f);
				}
			}
			return newPos;
		}
		public static Material ModifyHatForChild(Material mat, Pawn pawn){
			if (mat == null)
				return null;
			Material newMat = mat;
            if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child)
            {
                newMat.mainTexture.wrapMode = TextureWrapMode.Clamp;
				newMat.mainTextureOffset = new Vector2 (0, 0.018f);
			}
			return newMat;
		}
		public static bool EnsurePawnIsChildOrOlder(Pawn pawn){
			if(pawn.ageTracker.CurLifeStageIndex >= AgeStage.Child)
				return true;
			return false;
		}
		public static Material ModifyHairForChild(Material mat, Pawn pawn){
			Material newMat = mat;
			newMat.mainTexture.wrapMode = TextureWrapMode.Clamp;
			// Scale down the child hair to fit the head
			if (pawn.ageTracker.CurLifeStageIndex <= AgeStage.Child) {
				newMat.mainTextureScale = new Vector2 (1.13f, 1.13f);
				float benis = 0;
				if (!pawn.Rotation.IsHorizontal) {
					benis = -0.015f;
				}
				newMat.mainTextureOffset = new Vector2 (-0.045f + benis, -0.045f);
			}
			// Scale down the toddler hair to fit the head
			if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Toddler) {
				newMat.mainTextureOffset = new Vector2 (-0.07f, 0.12f);
			}
			return newMat;

		}
		public static Material ModifyClothingForChild(Material damagedMat, Pawn pawn, Rot4 bodyFacing){
            Material newDamagedMat= damagedMat;
            if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child && pawn.RaceProps.Humanlike) {
                const float ApperalTextureScaleX = 1.12f;
                const float ApperalTextureScaleY = 1.32f;
                const float ApperalTextureOffsetX = -0.055f;
                const float ApperalTextureOffsetY = -0.2f;
                const float ApperalTextureOffsetEWX = -0.06f;
                Material xDamagedMat = new Material(damagedMat);
                xDamagedMat.GetTexture("_MainTex").wrapMode = TextureWrapMode.Clamp;
                //
                //PutValue(pawn, ref ApperalTextureScaleX, ref ApperalTextureScaleY, ref ApperalTextureOffsetX, ref ApperalTextureOffsetY, ref ApperalTextureOffsetEWX);                 
                xDamagedMat.mainTextureScale = new Vector2(ApperalTextureScaleX, ApperalTextureScaleY);
                xDamagedMat.mainTextureOffset = new Vector2(ApperalTextureOffsetX, ApperalTextureOffsetY);
                if (bodyFacing == Rot4.West || bodyFacing == Rot4.East)
                    {  xDamagedMat.mainTextureOffset = new Vector2(ApperalTextureOffsetEWX, ApperalTextureOffsetY);  }
                
                newDamagedMat = xDamagedMat;
            }
            return newDamagedMat;
		}

        //
        public static void PutValue(Pawn pawn, ref float TextureScaleX, ref float TextureScaleY, ref float TextureOffsetX, ref float TextureOffsetY, ref float TextureOffsetEWX)
        {
            TextureScaleX = BnC_Settings.option_texture_scale_X;
            TextureScaleY = BnC_Settings.option_texture_scale_Y;
            TextureOffsetX = BnC_Settings.option_texture_offset_X;
            TextureOffsetY = BnC_Settings.option_texture_offset_Y;
            TextureOffsetEWX = BnC_Settings.option_texture_offset_westeast_X;
        }

    }
    
}

//      float TextureScaleX = 1.06f;
//      float TextureScaleY = 1.225f;
//      float TextureOffsetX = -0.024f;
//      float TextureOffsetY = -0.2f;
//      float TextureOffsetEWX = -0.015f;


//      float ApperalTextureScaleX = 1.1f;
//      float ApperalTextureScaleY = 1.225f;
//      float ApperalTextureOffsetX = -0.04f;
//      float ApperalTextureOffsetY = -0.2f;
//      float ApperalTextureOffsetEWX = -0.015f;
