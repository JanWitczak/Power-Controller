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
			listing_Standard.Label($"Desired Power Surplus: {Settings.DesiredRange.min}W - {Settings.DesiredRange.max}W");
			listing_Standard.IntRange(ref Settings.DesiredRange, 50, 5000);
			listing_Standard.Label($"Minimal Throtle: {Settings.MinimalThrotle * 100}%");
			Settings.MinimalThrotle = (float)Math.Round(listing_Standard.Slider(Settings.MinimalThrotle, 0.1f, 1.0f), 1);
			listing_Standard.CheckboxLabeled("Fill batteries first: ", ref Settings.FillBatteries, tooltip: "Fill batteries beffore throttling down generators.");
			listing_Standard.End();
			Settings.DesiredRange.min -= Settings.DesiredRange.min % 10;
			Settings.DesiredRange.max -= Settings.DesiredRange.max % 10;
		}
	}

	class PowerControllerSettings : ModSettings
	{
		public IntRange DesiredRange = new IntRange(500, 1500);
		public float MinimalThrotle = 0.1f;
		public bool FillBatteries = true;
		public override void ExposeData()
		{
			Scribe_Values.Look(ref DesiredRange, "DesiredRange", defaultValue: new IntRange(500, 1500));
			Scribe_Values.Look(ref MinimalThrotle, "MinimalThrotle", defaultValue: 0.1f);
			Scribe_Values.Look(ref FillBatteries, "FillBatteries", defaultValue: true);
		}
	}
}
