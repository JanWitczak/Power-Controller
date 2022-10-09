using HarmonyLib;
using System.Reflection;
using Verse;
using RimWorld;
using System.Text.RegularExpressions;

namespace PowerController
{
	[StaticConstructorOnStartup]
	static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("Azuraal.PowerController");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}

	[HarmonyPatch(typeof(CompPowerPlant), "UpdateDesiredPowerOutput")]
	class PowerPlantPatch
	{
		static void Postfix(ref CompPowerPlant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && __instance.PowerOutput != 0)
			{
				__instance.PowerOutput = (-__instance.Props.PowerConsumption) * Controller.Throttle;
			}
		}
	}

	[HarmonyPatch(typeof(CompRefuelable), "get_ConsumptionRatePerTick")]
	class RefuelableConsumptionRatePerTickPatch
	{
		static void Postfix(ref float __result, ref CompRefuelable __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__result *= Controller.Throttle;
			}
		}
	}

	[HarmonyPatch(typeof(CompRefuelable), "CompInspectStringExtra")]
	class RefuelableInspectPatch
	{
		private static Regex regex = new Regex("\\((.+)\\)");
		static void Postfix(ref string __result, ref CompRefuelable __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__result = regex.Replace(__result, $"({((int)(__instance.Fuel / __instance.Props.fuelConsumptionRate * 60000f / Controller.Throttle)).ToStringTicksToPeriod()})");
			}
		}
	}

	[HarmonyPatch(typeof(PowerNet), "PowerNetTick")]
	class PowerNetPatch
	{
		private static float Tolerance => PowerControllerMod.Settings.Tolerance;
		static void Postfix(ref PowerNet __instance)
		{
			float error = (__instance.CurrentEnergyGainRate() * 60000f) - PowerControllerMod.Settings.DesiredSurplus;
			if (PowerControllerMod.Settings.FillBatteries && !__instance.batteryComps.TrueForAll(x => x.StoredEnergyPct == 1.0f))
			{
				foreach (CompPower compPower in __instance.powerComps)
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
				foreach (CompPowerTrader compPower in __instance.powerComps)
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
