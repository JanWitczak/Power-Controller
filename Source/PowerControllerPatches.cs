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

	[HarmonyPatch(typeof(CompPowerPlant), "get_DesiredPowerOutput")]
	class PowerPlantPatch
	{
		static void Postfix(ref float __result, ref CompPowerPlant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && !Controller.Overriden)
			{
				__result *= Controller.Throttle;
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
}
