using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
		private float Step;
		private float StepPercentage;
		public double Throttle = 1.0;
		public double ThrottleTarget = 1.0;
		public bool AutomaticControl = true;
		public bool Overriden = false;
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
	public class CompInternalBattery : CompPowerBattery
	{
		public override string CompInspectStringExtra()
		{
			return "";
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			return Enumerable.Empty<Gizmo>();
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
	public class Gizmo_Throttle : Gizmo_Slider
	{
		public Gizmo_Throttle(CompPowerController compPowerController)
		{
			PowerController = compPowerController;
		}
		private CompPowerController PowerController;
		protected override float Target
		{
			get => (float)PowerController.ThrottleTarget;
			set => PowerController.ThrottleTarget = value;
		}

		protected override float ValuePercent => (float)PowerController.Throttle;
		protected override int Increments => 10;
		protected override string Title
		{
			get => "Throttle".Translate();
		}
		protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
		{
			base.DrawHeader(headerRect.LeftPart(0.8f), ref mouseOverElement);
			Widgets.CheckboxLabeled(headerRect.RightPart(0.2f), "", ref PowerController.AutomaticControl);
		}
		protected override bool IsDraggable
		{
			get
			{
				if (PowerController.parent.Faction == Faction.OfPlayer && !PowerController.AutomaticControl) return true;
				else return false;
			}
		}
		protected override FloatRange DragRange => new FloatRange(PowerControllerMod.Settings.MinimalThrotle, 1.0f);

		protected override string GetTooltip()
		{
			return "ThrottleDesc".Translate();
		}
	}
}