using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PowerController
{
	public class CompInternalBattery : CompPowerBattery
	{
		public override string CompInspectStringExtra()
		{
			return "";
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			return Enumerable.Empty<Gizmo>();
		}
		public override void PostExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				Scribe.saver.WriteElement("storedPower", StoredEnergy.ToString());
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				float ReadEnergy = 0;
				Scribe_Values.Look(ref ReadEnergy, "storedPower");
				AddEnergy(ReadEnergy);
			}
		}
	}
}
