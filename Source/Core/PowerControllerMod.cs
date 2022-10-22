using System;
using UnityEngine;
using Verse;

namespace PowerController
{
	[StaticConstructorOnStartup]
	class PowerControllerMod : Mod
	{
		public static PowerControllerSettings Settings;
		public PowerControllerMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<PowerControllerSettings>();
		}
		public override string SettingsCategory()
		{
			return "Power Controller";
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			listing_Standard.Label($"Desired Surplus: {Math.Max(Settings.DesiredSurplus - Settings.Tolerance, 0)}W - {Settings.DesiredSurplus + Settings.Tolerance}W");
			Settings.DesiredSurplus = (float)Math.Round(listing_Standard.Slider(Settings.DesiredSurplus / 100f, 1, 50), 0) * 100f;
			listing_Standard.Label($"Tolerance: {Settings.Tolerance}");
			Settings.Tolerance = (float)Math.Round(listing_Standard.Slider(Settings.Tolerance / 10f, 1, 50), 0) * 10f;
			listing_Standard.Label($"Minimal Throtle: {Settings.MinimalThrotle * 100}%");
			Settings.MinimalThrotle = (float)Math.Round(listing_Standard.Slider(Settings.MinimalThrotle, 0.1f, 1.0f), 1);
			listing_Standard.Label($"Maximal Throtle: {Settings.MaximalThrotle * 100}%");
			Settings.MaximalThrotle = (float)Math.Round(listing_Standard.Slider(Settings.MaximalThrotle, 1.0f, 1.5f), 1);
			listing_Standard.CheckboxLabeled("Fill batteries first: ", ref Settings.FillBatteries, tooltip:"Fill batteries beffore throttling down generators.");
			listing_Standard.End();
		}
	}

	class PowerControllerSettings : ModSettings
	{
		public float DesiredSurplus = 1000f;
		public float Tolerance = 10f;
		public float MinimalThrotle = 0.1f;
		public float MaximalThrotle = 1.0f;
		public bool FillBatteries = true;
		public override void ExposeData()
		{
			Scribe_Values.Look(ref DesiredSurplus, "DesiredSurplus", defaultValue: 1000f);
			Scribe_Values.Look(ref Tolerance, "Tolerance", defaultValue: 10f);
			Scribe_Values.Look(ref MinimalThrotle, "MinimalThrotle", defaultValue: 0.1f);
			Scribe_Values.Look(ref MaximalThrotle, "MaximalThrotle", defaultValue: 1.0f);
			Scribe_Values.Look(ref FillBatteries, "FillBatteries", defaultValue: true);
		}
	}
}
