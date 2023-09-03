using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrorTown;

namespace end360.TTT
{
    [Category("Equipment")]
    public partial class PortableTesterPlaced : ModelEntity, IUse
    {
        #region Console Variables
        [ConVar.Replicated("ttt_portable_tester_max_uses", Help = "Maximum number of uses the portable tester has.", Saved = true)]
        public static int MaxUses { get; set; } = 3;
        [ConVar.Replicated("ttt_portable_tester_detective_use", Help = "Should detectives be allowed to use the portable tester?", Saved = true)]
        public static bool DetectiveUsable { get; set; } = false;
        [ConVar.Server("ttt_portable_tester_broadcast_use", Help = "Should a popup message be sent to everyone with the test result? By default it only sends to the detectives.", Saved = true)]
        public static bool BroadcastMessage { get; set; } = false;
        [ConVar.Replicated("ttt_portable_tester_regenerate", Help = "Should uses of the portable tester regenerate over time?", Saved = true)]
        public static bool ShouldRecharge { get; set; } = false;
        [ConVar.Replicated("ttt_portable_tester_regenerate_time", Help = "How many seconds should it take to regenerate one use of the portable tester?", Saved = true)]
        public static float RechargeTime { get; set; } = 30;
        [ConVar.Replicated("ttt_portable_tester_multiple_use", Help = "Allow players to use the tester multiple times?", Saved = true)]
        public static bool AllowMultiUse { get; set; } = false;
        #endregion

        [Net]
        public int Uses { get; set; }
        [Net]
        public TimeSince TimeSinceLastRecharge { get; set; } = 0;

        TimeSince TimeSinceLastUse = 0;

        HashSet<TerrorTown.Player> Users { get; set; } = new(MaxUses);
        
        public override void Spawn()
        {
            base.Spawn();
            SetModel("models/microwave/microwave.vmdl");
            SetupPhysicsFromModel(PhysicsMotionType.Dynamic);
            Health = 200;
            PhysicsBody.Mass = 200;
            Uses = MaxUses;
            Tags.Add("solid", "canpush");
            RenderColor = Color.Blue;
        }

        [GameEvent.Tick.Server]
        public void TryRecharge()
        {
            if (ShouldRecharge && Uses < MaxUses && TimeSinceLastRecharge > RechargeTime)
            {
                Uses++;
                TimeSinceLastRecharge = 0;
            }
        }

        public bool HasUsed(Entity player)
        {
            if (player != null && Users != null)
            {
                return Users.Contains(player);
            }
            return false;
        }

        public override void TakeDamage(DamageInfo info)
        {
            if (info.HasTag("physics_impact")) return;
            base.TakeDamage(info);
            if (Game.IsServer && info.Attacker is TerrorTown.Player ply)
            {
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{ply.Client.Name} dealt {info.Damage} damage to a portable tester using {info.Weapon?.GetType().Name}.",
                    Icon = "error",
                    PlayersInvolved = new() { ply.Client.SteamId }
                });
            }
            else if (Game.IsServer)
            {
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{info.Attacker} dealt {info.Damage} damage to a portable tester using {info.Weapon?.GetType().Name}.",
                    Icon = "error"
                });
            }
        }
        public override void OnKilled()
        {
            Delete();
        }

        public bool IsUsable(Entity user) => Uses > 0
                && TimeSinceLastUse > 1f
                && user is TerrorTown.Player ply
                && ply.Team != null
                && (DetectiveUsable || ply.Team is not Detective)
                && (AllowMultiUse || !Users.Contains(ply));

        [Event("end360.ttt.player_tested")]
        public static void OnTest(PortableTesterPlaced tester, TerrorTown.Player ply, TeamAlignment alignment)
        {
            MyGame.Current.EventSystem.AddEventToLog(new BaseEvent()
            {
                EventString = $"{ply.Client.Name} tested as {alignment} using a portable tester.",
                Icon = "group",
                PlayersInvolved = new() { ply.Client.SteamId }
            });

            if (BroadcastMessage)
            {
                PopupSystem.DisplayPopup(To.Everyone, $"{ply.Client.Name} tested as {alignment}.", ply.Team.TeamColour, "Portable Tester");
            }
            else if (tester.Owner != null)
            {
                PopupSystem.DisplayPopup(To.Multiple(Game.Clients.Where(c =>
                {
                    if (c.Pawn is TerrorTown.Player ply)
                        return ply.Team is Detective;
                    return false;
                })), $"{ply.Client.Name} tested as {alignment}.", ply.Team.TeamColour, "Portable Tester");
            }

            if (alignment == TeamAlignment.Traitor)
            {
                tester.PlaySound("test negative").SetVolume(4);
            }
            else
            {
                tester.PlaySound("test positive").SetVolume(8);
            }
        }

        [ClientRpc]
        void OnUseClient(Entity user)
        {
            if (user is TerrorTown.Player ply)
                Users.Add(ply);
        }

        public bool OnUse(Entity user)
        {
            if (user is TerrorTown.Player ply && ply.Team != null)
            {
                Event.Run("end360.ttt.player_tested", this, ply, ply.Team.TeamAlignment);
                Users.Add(ply);
                OnUseClient(user); // I was trying to send it to just the person who used it but I can't seem to figure out how to get it to accept To
                
                Uses--;
                TimeSinceLastUse = 0;
                TimeSinceLastRecharge = 0;
                if (Uses <= 0 && !ShouldRecharge)
                {
                    Delete();
                }
            }

            return false;
        }
    }
}
