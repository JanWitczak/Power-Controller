using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PowerController
{
	public class CompPowerController : ThingComp
	{
		public float Throttle { get; private set; } = 1.0f;
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
			SetThrottle(Throttle + (10f / -PowerTrader.Props.basePowerConsumption));
			return 10f;
		}
		public float ThrottleDown()
		{
			SetThrottle(Throttle - (10f / -PowerTrader.Props.basePowerConsumption));
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
	}
}
