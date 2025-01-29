using HarmonyLib;
using RimWorld;
using SaveOurShip2;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;

namespace PowerController
{
	[StaticConstructorOnStartup]
	static class SaveOurShipHarmonyPatches
	{
		static SaveOurShipHarmonyPatches()
		{
			Harmony harmony = new Harmony("Azuraal.PowerController.SaveOurShipIntegration");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}
	[HarmonyPatch(typeof(CompPowerTraderOverdrivable), "FlickOverdrive")]
	class ReactorPowerPlantOutputPatch
	{
		static void Postfix(ref CompPowerTraderOverdrivable __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				if (__instance.overdriveSetting == 0)
				{
					Controller.Overriden = false;
				}
				else
				{
					Controller.Overriden = true;
					Controller.SetThrottle(1.0);
				}
			}
		}
	}
	[HarmonyPatch]
	class RefuelableOverdrivableFuelPatch
	{
		public static MethodBase TargetMethod()
		{
			return AccessTools.PropertyGetter(AccessTools.TypeByName("SaveOurShip2.CompRefuelableOverdrivable"), "ConsumptionRatePerTick");
		}
		static void Postfix(ref float __result, ref CompPowerTraderOverdrivable __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && !Controller.Overriden)
			{
				__result = (float)((double)__result * Controller.Throttle);
			}
		}
	}
	[HarmonyPatch]
	class RefuelableOverdrivableStringPatch
	{
		public static MethodBase TargetMethod()
		{
			return AccessTools.TypeByName("SaveOurShip2.CompRefuelableOverdrivable").GetMethod("CompInspectStringExtra");
		}
		private static Regex regex = new Regex("\\((.+)\\)");
		static void Postfix(ref string __result, ref CompRefuelable __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && !Controller.Overriden)
			{
				__result = regex.Replace(__result, $"({((int)(__instance.Fuel / __instance.Props.fuelConsumptionRate * 60000f / Controller.Throttle)).ToStringTicksToPeriod()})");
			}
		}
	}
}