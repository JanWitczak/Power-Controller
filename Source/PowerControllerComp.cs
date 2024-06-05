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
					double error = ((double)powerNet.CurrentEnergyGainRate() * 60000d) - PowerControllerMod.Settings.DesiredSurplus;
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
		public double Throttle = 1.0f;
		public bool Overriden = false;
		public float ThrottleOverride = 0.0f;
		private CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			if (-PowerTrader.Props.PowerConsumption >= 10000f) Step = 50.0f;
			if (-PowerTrader.Props.PowerConsumption >= 5000f) Step = 25.0f;
			else Step = 10.0f;
			StepPercentage = Step / -PowerTrader.Props.PowerConsumption;
		}
		public double SetThrottle(double throttle)
		{
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
			else return $"Throttle: {((float)Throttle).ToStringPercent()}";
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
