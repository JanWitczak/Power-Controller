using HarmonyLib;
using PipeSystem;
using System.Reflection;
using Verse;

namespace PowerController
{
	[StaticConstructorOnStartup]
	static class VEHelixienHarmonyPatches
	{
		static VEHelixienHarmonyPatches()
		{
			Harmony harmony = new Harmony("Azuraal.PowerController.VEHelixienIntegration");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}

	[HarmonyPatch(typeof(CompResourceTrader), "get_BaseConsumption")]
	class HelixienPowerPlantPatch
	{
		static void Postfix(ref float __result, ref CompResourceTrader __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null)
			{
				__result *= (float)Controller.Throttle;
				if (Controller.ThrottleChanged)
				{
					__instance.UsedLastTick = true;
					Controller.ThrottleChanged = false;
				}
			}
		}
	}
}