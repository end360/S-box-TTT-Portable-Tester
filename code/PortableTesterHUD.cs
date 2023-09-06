using Sandbox;
using System;
using TerrorTown;

namespace end360.TTT
{
    public partial class PortableTesterHUD
    {
        static PortableTesterPlaced? Tester => Game.LocalPawn?.Components.Get<PortableTesterInspectionComponent>()?.LookingAt;
        static bool ShouldDraw => Tester != null && Tester.IsValid;
        static bool ShouldDrawRecharge => ShouldDraw && PortableTesterPlaced.ShouldRecharge && Tester!.Uses < PortableTesterPlaced.MaxUses;
        static int RechargeTime => (int) Math.Round(PortableTesterPlaced.RechargeTime - Tester?.TimeSinceLastRecharge ?? 0);

        static PortableTesterHUD? Instance;
        /// <summary>
        /// Rebuilds the HUD, if the current instance is valid it will be deleted.
        /// </summary>
        public static void RebuildHUD()
        {
            if(!Game.IsClient)
            {
                Log.Error("RebuildHUD called on non-client.");
                return;
            }
            if (Instance != null && Instance.IsValid)
                Instance.Delete();

            Instance = new()
            {
                Parent = HUDRootPanel.Current
            };
        }

        protected override int BuildHash() => System.HashCode.Combine(Instance, Tester?.Uses, ShouldDraw, ShouldDrawRecharge ? RechargeTime : -1, PortableTesterPlaced.AllowMultiUse, PortableTesterPlaced.DetectiveUsable, (Game.LocalPawn as TerrorTown.Player)?.Team);

        /// <summary>
        /// Attempts to rebuild the HUD if it needs to be rebuilt (e.g. the instance is null or invalid.) Currently runs every frame because that's the easiest way I could think of to re-create it whenever the TTT hud root is re-created (it'd be nice to have an event for this).
        /// </summary>
        [GameEvent.Client.Frame]
        static void TryRebuildHUD()
        {
            if (Instance == null || !Instance.IsValid || Instance.Parent != HUDRootPanel.Current)
                RebuildHUD();
        }

        [Event.Hotload]
        static void HotLoaded()
        {
            if (Game.IsClient)
                RebuildHUD();
        }
    }
}
