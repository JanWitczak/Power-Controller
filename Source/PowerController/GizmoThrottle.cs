using RimWorld;
using UnityEngine;
using Verse;

namespace PowerController
{
	public class Gizmo_Throttle : Gizmo_Slider
	{
		private static bool draggingBar = false;
		public Gizmo_Throttle(CompPowerController compPowerController)
		{
			PowerController = compPowerController;
		}
		private CompPowerController PowerController;
		protected override float Target
		{
			get => (float)PowerController.ThrottleTarget;
			set => PowerController.ThrottleTarget = value;
		}

		protected override bool DraggingBar
		{
			get => draggingBar;
			set => draggingBar = value;
		}

		protected override float ValuePercent => (float)PowerController.Throttle;
		protected override int Increments => 10;
		protected override string Title
		{
			get => "Throttle".Translate();
		}
		protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
		{
			base.DrawHeader(headerRect.LeftPart(0.8f), ref mouseOverElement);
			Widgets.CheckboxLabeled(headerRect.RightPart(0.2f), "", ref PowerController.AutomaticControl);
		}
		protected override bool IsDraggable
		{
			get => (PowerController.parent.Faction == Faction.OfPlayer && !PowerController.AutomaticControl);
		}
		protected override FloatRange DragRange => new FloatRange(PowerControllerMod.Settings.MinimalThrotle, 1.0f);

		protected override string GetTooltip()
		{
			return "ThrottleDesc".Translate();
		}
	}
}
