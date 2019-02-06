using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;

namespace RimWorldChildren
{

	public class WorkGiver_FeedBaby : WorkGiver_Scanner
	{
		//
		// Properties
		//
		public override PathEndMode PathEndMode {
			get {
				return PathEndMode.Touch;
			}
		}
		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup (ThingRequestGroup.Pawn);
			}
		}

		//
		// Methods
		//
		public override bool HasJobOnThing (Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = t as Pawn;
			if (pawn2 == null || pawn2 == pawn) {
				return false;
			}
			if (!pawn2.RaceProps.Humanlike || pawn2.ageTracker.CurLifeStageIndex > AgeStage.Toddler) {
				return false;
			}
			if (pawn2.needs.food == null || pawn2.needs.food.CurLevelPercentage > pawn2.needs.food.PercentageThreshHungry + 0.02) {
				return false;
			}
			if (!pawn2.InBed()){
				return false;
			}
			if (!FeedPatientUtility.ShouldBeFed (pawn2)) {
				return false;
			}
			if (!pawn.CanReserveAndReach (t, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)) {
				return false;
			}
			Thing thing;
			ThingDef thingDef;
			if (!FoodUtility.TryFindBestFoodSourceFor(pawn, pawn2, pawn2.needs.food.CurCategory == HungerCategory.UrgentlyHungry, out thing, out thingDef, false, true, false, false, false) && !ChildrenUtility.CanBreastfeed(pawn))
			{
				JobFailReason.Is("NoFood".Translate());
				return false;
			}
			return true;
		}
		
		// We just use the FeedPatient Job from the medical branch for non-breastfeeding
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn pawn2 = (Pawn)t;
			if (pawn2 != null){
				if(ChildrenUtility.CanBreastfeed(pawn)){
					//Log.Message("Deciding to breastfeed baby.");
					return new Job (DefDatabase<JobDef>.GetNamed ("BreastfeedBaby")) {
						targetA = pawn2,
					};
				}
				else{
					Thing t2;
					ThingDef def1;
					if (FoodUtility.TryFindBestFoodSourceFor(pawn, pawn2, pawn2.needs.food.CurCategory == HungerCategory.UrgentlyHungry, out t2, out def1, false, true, false, false, false))
					{
						//Log.Message("Deciding to feed normal food to baby.");
						return new Job(JobDefOf.FeedPatient)
						{
							targetA = t2,
							targetB = pawn2,
							count = FoodUtility.WillIngestStackCountOf(pawn2, def1, t2.def.ingestible.CachedNutrition)
						};
					}
				}
			}
			return null;
		}
	}
	
	public class JobDriver_BreastfeedBaby : JobDriver
	{
		//
		// Static Fields
		//
		private const int breastFeedDuration = 600;

		//
		// Properties
		//
		protected Pawn Victim {
			get {
				return (Pawn)TargetA.Thing;
			}
		}
		
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.Victim, this.job, 1, -1, null);
		}

//		public override void ExposeData ()
//		{
//			base.ExposeData ();
//		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull (TargetIndex.A);
			this.FailOnSomeonePhysicallyInteracting (TargetIndex.A);
			this.FailOn(delegate {
				if(!ChildrenUtility.CanBreastfeed (pawn) || !pawn.CanReserve (TargetA, 1, -1, null, false))
					return true;
				else return false;
			});

			yield return Toils_Reserve.Reserve (TargetIndex.A, 1, -1, null);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);
			Toil prepare = new Toil();
			prepare.initAction = delegate
			{
				PawnUtility.ForceWait(Victim, breastFeedDuration, Victim);
			};
			prepare.defaultCompleteMode = ToilCompleteMode.Delay;
			prepare.defaultDuration = breastFeedDuration;
			yield return prepare;
			yield return new Toil
			{
				initAction = delegate
				{
					AddEndCondition (() => JobCondition.Succeeded);
					// Baby is full
					Victim.needs.food.CurLevelPercentage = 1f;
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}