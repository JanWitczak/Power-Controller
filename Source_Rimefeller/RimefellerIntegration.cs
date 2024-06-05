using HarmonyLib;
using Rimefeller;
using System.Reflection;
using System.Text.RegularExpressions;
using Verse;
using RimWorld;

namespace PowerController
{
	[StaticConstructorOnStartup]
	static class RimefellerHarmonyPatches
	{
		static RimefellerHarmonyPatches()
		{
			Harmony harmony = new Harmony("Azuraal.PowerController.RimefellerIntegration");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}

	[HarmonyPatch(typeof(FuelPowerplant), "get_fuelPerTick")]
	class FueledPlantConsuptionPatch
	{
		static void Postfix(ref float __result, ref FuelPowerplant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__result *= (float)Controller.Throttle;
			}
		}
	}
	[HarmonyPatch(typeof(FuelPowerplant), "UpdateOutput")]
	class FueledPlantOutputPatch
	{
		static void Postfix(ref FuelPowerplant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__instance.PowerOutput = (float)((double)__instance.PowerOutput * Controller.Throttle);
			}
		}
	}
	[HarmonyPatch(typeof(FuelPowerplant), "CompInspectStringExtra")]
	class FueledPlantInspectorStringPatch
	{
		private static Regex regex = new Regex(": (\\d+) L/");
		static void Postfix(ref string __result, ref FuelPowerplant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__result = regex.Replace(__result, $": {(__instance.Props.FuelRate * Controller.Throttle).ToString("0.0")} L/");
			}
		}
	}
}