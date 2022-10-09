using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PowerController
{
	public class CompPowerController : ThingComp
	{
		public float Throttle = 1.0f;
		private CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();
		public void SetThrottle(float throttle)
		{
			Throttle = throttle;
			if (Throttle < PowerControllerMod.Settings.MinimalThrotle)
			{
				Throttle = PowerControllerMod.Settings.MinimalThrotle;
			}
			if (Throttle > PowerControllerMod.Settings.MaximalThrotle)
			{
				Throttle = PowerControllerMod.Settings.MaximalThrotle;
			}
		}
		public float ThrottleUp()
		{
			SetThrottle(Throttle + (10f / -PowerTrader.Props.PowerConsumption));
			return 10f;
		}
		public float ThrottleDown()
		{
			SetThrottle(Throttle - (10f / -PowerTrader.Props.PowerConsumption));
			return -10f;
		}
		public bool IsMaxThrottle()
		{
			if (Throttle == PowerControllerMod.Settings.MaximalThrotle) return true;
			else return false;
		}
		public bool IsMinThrottle()
		{
			if (Throttle == PowerControllerMod.Settings.MinimalThrotle) return true;
			else return false;
		}
		public override string CompInspectStringExtra()
		{
			return $"Throttle: {Throttle.ToStringPercent()}";
		}
		public override void PostExposeData()
		{
			Scribe_Values.Look(ref Throttle, "throttle");
		}
	}

	public class CompInternalBattery : CompPowerBattery
	{
		public override string CompInspectStringExtra()
		{
			return "";
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			return new List<Gizmo>();
		}
		public override void PostExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				Scribe.saver.WriteElement("storedPower", StoredEnergy.ToString());
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				float ReadEnergy = 0;
				Scribe_Values.Look(ref ReadEnergy, "storedPower");
				AddEnergy(ReadEnergy);
			}
		}
	}
}
