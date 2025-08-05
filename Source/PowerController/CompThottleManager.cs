using RimWorld;
using System.Linq;
using Verse;

namespace PowerController
{
	public class ThrottleManager : MapComponent
	{
		private static readonly int BaseTickDelay = 10;
		private static readonly int LongTickDelay = 250;
		private static float Tolerance => PowerControllerMod.Settings.Tolerance;
		private int[] PowerNetDelays = null;
		public ThrottleManager(Map map) : base(map)
		{
		}
		public override void MapComponentTick()
		{
			if (PowerNetDelays == null || map.powerNetManager.AllNetsListForReading.Count != PowerNetDelays.Length)
			{
				PowerNetDelays = new int[map.powerNetManager.AllNetsListForReading.Count];
			}

			for (int powerNetIndex = 0; powerNetIndex < PowerNetDelays.Length; powerNetIndex++)
			{
				if (PowerNetDelays[powerNetIndex] > 0)
				{
					PowerNetDelays[powerNetIndex]--;
					continue;
				}
				PowerNet powerNet = map.powerNetManager.AllNetsListForReading[powerNetIndex];
				if (powerNet.HasActivePowerSource)
				{
					double error = ((double)powerNet.CurrentEnergyGainRate() * 60000d) - PowerControllerMod.Settings.DesiredSurplus;
					if (PowerControllerMod.Settings.FillBatteries && powerNet.batteryComps.Any(x => x.AmountCanAccept > 0.0f)) AdjustNetwork(powerNet, powerNetIndex, error, ThrottleAction.Charge);
					else if (!NetworkIsNominal(error)) AdjustNetwork(powerNet, powerNetIndex, error, ThrottleAction.Balance);
				}
			}
		}
		private bool NetworkIsNominal(double error)
		{
			if (error > Tolerance || error < -Tolerance || error + PowerControllerMod.Settings.DesiredSurplus < 0) return false;
			else return true;
		}
		private void AdjustNetwork(PowerNet powerNet, int powerNetIndex, double error, ThrottleAction action)
		{
			bool AtCapacity = true;
			foreach (CompPowerTrader compPower in powerNet.powerComps)
			{
				CompPowerController Controller = compPower.parent.GetComp<CompPowerController>();
				if (Controller != null)
				{
					if (error > 0 && !Controller.IsMinThrottle())
					{
						error += Controller.ThrottleDown();
						if (!Controller.IsMinThrottle()) AtCapacity = false;
					}
					else if (error < 0 && !Controller.IsMaxThrottle())
					{
						error += Controller.ThrottleUp();
						if (!Controller.IsMaxThrottle()) AtCapacity = false;
					}
					if (NetworkIsNominal(error)) break;
				}
			}
			if (AtCapacity) PowerNetDelays[powerNetIndex] = LongTickDelay;
			else PowerNetDelays[powerNetIndex] = BaseTickDelay;
		}

		private enum ThrottleAction
		{
			Balance,
			Charge
		}
	}
}