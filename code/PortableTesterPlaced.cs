using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [ConVar.Server("ttt_portable_tester_fake", Help = "Should traitors be allowed to fake the next test?", Saved = true)]
        public static bool AllowFakingTest { get; set; } = true;
        [ConVar.Server("ttt_portable_tester_fake_recharge", Help = "Should the traitor button for the fake test recharge?", Saved = true)]
        public static bool RechargeFakeTest { get; set; } = false;
        [ConVar.Server("ttt_portable_tester_fake_recharge_time", Help = "How many seconds should it take for the fake test to recharge?", Saved = true)]
        public static float FakeTestRechargeTime { get; set; } = 30;
        #endregion
        #region Networked Variables
        [Net]
        public int Uses { get; set; }
        [Net]
        public TimeUntil TimeUntilRecharge { get; set; } = 0;
        #endregion
        #region Properties
        public PointLightEntity? PointLight => Children.OfType<PointLightEntity>().FirstOrDefault();
        public RoleButton? RoleButton => Children.OfType<RoleButton>().FirstOrDefault();
        #endregion
        #region Variables
        TimeUntil DisableLight = 0;
        TimeUntil UseTime = 0;
        TimeUntil FakeTestRecharge = 0;
        bool FakeNextTest = false;
        readonly HashSet<TerrorTown.Player> Users = new(MaxUses);
        #endregion

        #region Public Methods
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

            var light = new PointLightEntity()
            {
                Enabled = false,
                DynamicShadows = false,
                Range = 96,
                Brightness = 1
            };
            light.SetParent(this);

            CreateRoleButton();
        }

        public bool HasUsed(Entity player)
        {
            if (player != null && Users != null)
            {
                return Users.Contains(player);
            }
            return false;
        }

        public bool IsUsable(Entity user) => Uses > 0
                && UseTime
                && user is TerrorTown.Player ply
                && ply.Team != null
                && (DetectiveUsable || ply.Team is not Detective)
                && (AllowMultiUse || !Users.Contains(ply));

        public override void OnKilled()
        {
            Delete();
        }

        public bool OnUse(Entity user)
        {
            if (user is TerrorTown.Player ply && ply.Team != null)
            {
                Event.Run("end360.ttt.player_tested", this, ply, FakeNextTest);
                OnTest(ply, FakeNextTest);
                FakeNextTest = false;
                Users.Add(ply);
                OnUseClient(user); // I was trying to send it to just the person who used it but I can't seem to figure out how to get it to accept To

                Uses--;
                UseTime = 3;
                if (TimeUntilRecharge)
                    TimeUntilRecharge = RechargeTime;
                if (Uses <= 0 && !ShouldRecharge)
                {
                    Delete();
                }
            }

            return false;
        }

        public void OnTest(TerrorTown.Player ply, bool isFakeTest)
        {
            var alignment = isFakeTest ? (ply.Team.TeamAlignment == TeamAlignment.Innocent ? TeamAlignment.Traitor : TeamAlignment.Innocent) : ply.Team.TeamAlignment;
            var color = alignment == TeamAlignment.Innocent ? Color.Green : Color.Red;
            MyGame.Current.EventSystem.AddEventToLog(new BaseEvent()
            {
                EventString = $"{ply.Client.Name} tested as {alignment}{(isFakeTest ? " (faked)" : "")} using a portable tester.",
                Icon = "group",
                PlayersInvolved = new() { ply.Client.SteamId }
            });

            var MessageFilter = BroadcastMessage ? To.Everyone : To.Multiple(Teams.Get<Detective>().Players.Select(p => p.Client));
            PopupSystem.DisplayPopup(MessageFilter, $"{ply.Client.Name} tested as {alignment}.", color, "Portable Tester");

            var sound = alignment == TeamAlignment.Traitor ? "test negative" : "test positive";
            PlaySound(sound).SetVolume(8);

            if (PointLight != null)
            {
                PointLight.Color = color;
                PointLight.Enabled = true;
                DisableLight = 1f;
            }
        }

        public override void TakeDamage(DamageInfo info)
        {
            if (info.HasTag("physics_impact")) return;

            base.TakeDamage(info);
            if (!Game.IsServer) return;

            if (info.Attacker is TerrorTown.Player ply)
            {
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{ply.Client.Name} dealt {Math.Round(info.Damage)} damage to a portable tester using {info.Weapon?.GetType().Name}.",
                    Icon = "error",
                    PlayersInvolved = new() { ply.Client.SteamId }
                });
            }
            else if (info.Attacker.Owner is TerrorTown.Player ply2)
            {
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{ply2.Client.Name} dealt {Math.Round(info.Damage)} damage to a portable tester using {info.Weapon?.GetType().Name ?? info.Attacker?.GetType().Name}.",
                    Icon = "error"
                });
            } else
            {
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{info.Attacker} dealt {Math.Round(info.Damage)} damage to a portable tester.",
                    Icon = "error"
                });
            }
        }

        #endregion

        void CreateRoleButton()
        {
            if (AllowFakingTest)
            {
                var btn = new RoleButton()
                {
                    EnableDrawing = false,
                    Description = "Fake the next test",
                    RemoveOnUse = true,
                    Radius = 1150,
                    RoleName = "Traitor"
                };
                btn.SetParent(this);
                btn.LocalPosition = Vector3.Up * 48;
                btn.AddOutputEvent("OnPressed", OnTestFaked);
            }
        }

        /// <summary>
        /// Called when someone uses a portable tester.
        /// </summary>
        /// <param name="user"></param>
        [ClientRpc]
        void OnUseClient(Entity user)
        {
            if (user is TerrorTown.Player ply)
                Users.Add(ply);
        }

        /// <summary>
        /// Called when a traitor activates the role button to fake the next test.
        /// </summary>
        /// <param name="activator"></param>
        /// <param name="delay"></param>
        /// <returns>ValueTask.CompletedTask</returns>
        ValueTask OnTestFaked(Entity activator, float delay)
        {
            if (AllowFakingTest && activator is TerrorTown.Player ply)
            {
                FakeNextTest = true;
                FakeTestRecharge = FakeTestRechargeTime;
                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{ply.Client.Name} set it so that {this} will fake (invert the outcome of) the next test.",
                    PlayersInvolved = new() { ply.Client.Id }
                });
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Recharge the tester, if it should be recharged, and disable the light if it should be disabled.
        /// </summary>
        [GameEvent.Tick.Server]
        void Tick()
        {
            if (ShouldRecharge && Uses < MaxUses && TimeUntilRecharge)
            {
                Uses++;
                if(Uses != MaxUses)
                    TimeUntilRecharge = RechargeTime;
            }

            if(RechargeFakeTest && (RoleButton == null || !RoleButton.IsValid ) && FakeTestRecharge)
            {
                CreateRoleButton();
            }

            if (DisableLight && PointLight != null)
            {
                PointLight.Enabled = false;
            }


        }
    }
}
