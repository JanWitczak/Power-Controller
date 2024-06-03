using RimWorld;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Verse;

namespace PowerController
{
	public class ThrottleManager : MapComponent
	{
		private static float Tolerance => PowerControllerMod.Settings.Tolerance;
		public ThrottleManager(Map map) : base(map)
		{

		}
		public override void MapComponentTick()
		{
			foreach (PowerNet powerNet in map.powerNetManager.AllNetsListForReading)
			{
				if (powerNet.HasActivePowerSource)
				{
					float error = (powerNet.CurrentEnergyGainRate() * 60000f) - PowerControllerMod.Settings.DesiredSurplus;
					if (PowerControllerMod.Settings.FillBatteries && powerNet.batteryComps.Any(x => x.AmountCanAccept > 0.0f))
					{
						foreach (CompPower compPower in powerNet.powerComps)
						{
							CompPowerController Controller = compPower.parent.GetComp<CompPowerController>();
							if (Controller != null && !Controller.IsMaxThrottle())
							{
								Controller.ThrottleUp();
							}
						}
					}
					else if (error > Tolerance || error < -Tolerance || error + PowerControllerMod.Settings.DesiredSurplus < 0)
					{
						foreach (CompPowerTrader compPower in powerNet.powerComps)
						{
							CompPowerController Controller = compPower.parent.GetComp<CompPowerController>();
							if (Controller != null)
							{
								if (error > 0 && !Controller.IsMinThrottle()) error += Controller.ThrottleDown();
								else if (error < 0 && !Controller.IsMaxThrottle()) error += Controller.ThrottleUp();
								if (error < Tolerance && error > Tolerance && error + PowerControllerMod.Settings.DesiredSurplus > 0) break;
							}
						}
					}
				}
			}
		}
	}
	public class CompPowerController : ThingComp
	{
		public float Step;
		private float StepPercentage;
		public float Throttle = 1.0f;
		public bool Overriden = false;
		public float ThrottleOverride = 0.0f;
		private CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			if (-PowerTrader.Props.PowerConsumption >= 10000f) Step = 1000.0f;
			if (-PowerTrader.Props.PowerConsumption >= 5000f) Step = 100.0f;
			else Step = 10.0f;
			StepPercentage = Step / -PowerTrader.Props.PowerConsumption;
		}
		public float SetThrottle(float throttle)
		{
			float adjustment = Throttle;
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
		public float ThrottleUp()
		{
			float adjustment = SetThrottle(Throttle + StepPercentage);
			return Step * (adjustment / StepPercentage);
		}
		public float ThrottleDown()
		{
			float adjustment = SetThrottle(Throttle - StepPercentage);
			return Step * (adjustment / StepPercentage);
		}
		public bool IsMaxThrottle()
		{
			if (Throttle == 1.0f) return true;
			else return false;
		}
		public bool IsMinThrottle()
		{
			if (Throttle == PowerControllerMod.Settings.MinimalThrotle) return true;
			else return false;
		}
		public override string CompInspectStringExtra()
		{
			if (Overriden) return $"Throttle: {ThrottleOverride.ToStringPercent()} - Overriden";
			else return $"Throttle: {Throttle.ToStringPercent()}";
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
