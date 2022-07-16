using HarmonyLib;
using System.Reflection;
using Verse;
using RimWorld;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PowerController
{
	public class HarmonyPatches : Verse.Mod
	{
		public HarmonyPatches(ModContentPack content) : base(content)
		{
			var harmony = new Harmony("Azuraal.PowerController");
			var assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}

		[HarmonyPatch(typeof(CompPowerPlant), "UpdateDesiredPowerOutput")]
		class PowerPlantPatch
		{
			static void Postfix(ref CompPowerPlant __instance)
			{
				CompPowerController Controller = __instance.parent.GetComp<CompPowerController>();
				if (Controller != null && __instance.PowerOutput != 0)
				{
					__instance.PowerOutput = (-__instance.Props.basePowerConsumption) * Controller.Throttle;
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
				else if (error > Tolerance || error < -Tolerance)
				{
					List<CompPowerController> AvailibleGenerators = new List<CompPowerController>();
					foreach (CompPowerTrader compPower in __instance.powerComps)
					{
						CompPowerController Controller = compPower.parent.GetComp<CompPowerController>();
						if (Controller != null)
						{
							if (error > 0 && !Controller.IsMinThrottle()) AvailibleGenerators.Add(Controller);
							else if (error < 0 && !Controller.IsMaxThrottle()) AvailibleGenerators.Add(Controller);
						}
						else if (!compPower.PowerOn && compPower.parent.GetComp<CompFlickable>().SwitchIsOn) error -= compPower.PowerOutput;
					}
					foreach (CompPowerController Controller in AvailibleGenerators)
					{
						if (error > 0)
						{
							error += Controller.ThrottleDown();
						}
						else if (error < 0)
						{
							error += Controller.ThrottleUp();
						}
						if (error == 0) break;
					}
				}
			}
		}
	}
}
