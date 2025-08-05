using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace PowerController
{
	public class CompPowerController : ThingComp
	{
		private float Step;
		private float StepPercentage;
		public double Throttle = 1.0;
		public double ThrottleTarget = 1.0;
		public bool AutomaticControl = true;
		public bool Overriden = false;
		public bool ThrottleChanged = false;
		private CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();
		private Gizmo_Throttle Gizmo;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			if (-PowerTrader.Props.PowerConsumption >= 10000f) Step = 50.0f;
			else if (-PowerTrader.Props.PowerConsumption >= 5000f) Step = 25.0f;
			else Step = 10.0f;
			StepPercentage = Step / -PowerTrader.Props.PowerConsumption;
			Gizmo = new Gizmo_Throttle(this);
		}
		public double SetThrottle(double throttle)
		{
			ThrottleChanged = true;
			double adjustment = Throttle;
			Throttle = throttle;
			if (Throttle < PowerControllerMod.Settings.MinimalThrotle)
			{
				Throttle = PowerControllerMod.Settings.MinimalThrotle;
			}
			if (Throttle > 1.0f)
			{
				Throttle = 1.0f;
			}
			adjustment -= Throttle;
			return -adjustment;
		}
		public double ThrottleUp()
		{
			double adjustment = SetThrottle(Throttle + StepPercentage);
			return Step * (adjustment / StepPercentage);
		}
		public double ThrottleDown()
		{
			double adjustment = SetThrottle(Throttle - StepPercentage);
			return Step * (adjustment / StepPercentage);
		}
		public bool IsMaxThrottle()
		{
			if (Throttle == 1.0f || !AutomaticControl || Overriden) return true;
			else return false;
		}
		public bool IsMinThrottle()
		{
			if (Throttle == PowerControllerMod.Settings.MinimalThrotle || !AutomaticControl || Overriden) return true;
			else return false;
		}
		public override void CompTick()
		{
			if (!AutomaticControl)
			{
				if (ThrottleTarget > Throttle + StepPercentage / 2) ThrottleUp();
				else if (ThrottleTarget < Throttle - StepPercentage / 2) ThrottleDown();
			}
			else ThrottleTarget = Math.Round(Throttle, 1);
		}
		public override void PostExposeData()
		{
			Scribe_Values.Look(ref Throttle, "throttle", defaultValue: 1.0);
			Scribe_Values.Look(ref ThrottleTarget, "target", defaultValue: 1.0);
			Scribe_Values.Look(ref AutomaticControl, "automatic", defaultValue: true);
			Scribe_Values.Look(ref Overriden, "overriden", defaultValue: false);
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			yield return Gizmo;
		}
	}
}
