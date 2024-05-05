using HarmonyLib;
using VanillaPowerExpanded;
using System.Reflection;
using Verse;

namespace PowerController
{
	[StaticConstructorOnStartup]
	static class VEPowerHarmonyPatches
	{
		static VEPowerHarmonyPatches()
		{
			Harmony harmony = new Harmony("Azuraal.PowerController.VEPowerIntegration");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}

	[HarmonyPatch(typeof(CompPowerPlantNuclear), "UpdateDesiredPowerOutput")]
	class NuclearPowerPlantPatch
	{
		static void Postfix(ref CompPowerPlantNuclear __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && __instance.PowerOutput != 0)
			{
				__instance.PowerOutput = __instance.PowerOutput * Controller.Throttle;
			}
		}
	}
	
}