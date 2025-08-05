using RimWorld;
using System.Linq;
using Verse;

namespace PowerController
{
	public class ThrottleManager : MapComponent
	{
		private static readonly int BaseTickDelay = 10;
		private static readonly int LongTickDelay = 250;
		private static IntRange DesiredSurplus => PowerControllerMod.Settings.DesiredRange;
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
					bool chargeBatteries = false;
					if (PowerControllerMod.Settings.FillBatteries && powerNet.batteryComps.Any(x => x.AmountCanAccept > 0.0f))
					{
						chargeBatteries = true;
					}
					double networkEnergyGainRate = ((double)powerNet.CurrentEnergyGainRate() * 60000d);
					if (chargeBatteries || !NetworkIsNominal(networkEnergyGainRate))
					{
						bool AtCapacity = true;
						foreach (CompPowerTrader compPower in powerNet.powerComps)
						{
							CompPowerController Controller = compPower.parent.GetComp<CompPowerController>();
							if (Controller != null)
							{
								if (!Controller.IsMinThrottle() && !chargeBatteries && networkEnergyGainRate > DesiredSurplus.max)
								{
									networkEnergyGainRate += Controller.ThrottleDown();
									if (!Controller.IsMinThrottle()) AtCapacity = false;
								}
								else if (!Controller.IsMaxThrottle() && (chargeBatteries || networkEnergyGainRate < DesiredSurplus.min))
								{
									networkEnergyGainRate += Controller.ThrottleUp();
									if (!Controller.IsMaxThrottle()) AtCapacity = false;
								}
								if (NetworkIsNominal(networkEnergyGainRate)) break;
							}
						}
						if (AtCapacity) PowerNetDelays[powerNetIndex] = LongTickDelay;
						else PowerNetDelays[powerNetIndex] = BaseTickDelay;
					}
				}
			}
		}
		private bool NetworkIsNominal(double EnergyGainRate)
		{
			if (EnergyGainRate > DesiredSurplus.max || EnergyGainRate < DesiredSurplus.min) return false;
			else return true;
		}
		private enum ThrottleAction
		{
			Balance,
			Charge
		}
	}
}