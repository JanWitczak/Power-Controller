using HarmonyLib;
using VanillaPowerExpanded;
using GasNetwork;
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

	[HarmonyPatch(typeof(CompGasPowerPlant), "CompTick")]
	class GasPowerPlantPatch
	{
		static void Postfix(ref CompGasPowerPlant __instance)
		{
			CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
			if (Controller != null && __instance.PowerOutput != 0)
			{
				__instance.PowerOutput = (-__instance.Props.basePowerConsumption) * Controller.Throttle;
				__instance.parent.GetComp<CompGasTrader>().GasConsumption = __instance.parent.GetComp<CompGasTrader>().Props.gasConsumption * Controller.Throttle;
			}
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