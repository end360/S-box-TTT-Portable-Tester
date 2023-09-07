using Editor;
using Sandbox;
using TerrorTown;

namespace end360.TTT
{
    [Category("Equipment")]
    [DetectiveBuyable("Special", 1, 1)]
    [EditorModel("models/microwave/microwave.vmdl")]
    [HammerEntity]
    [Library("ttt_equipment_portabletester")]
    [Title("Portable Tester")]
    public class PortableTester : Carriable
    {
        public override string WorldModelPath => "models/microwave/microwave.vmdl";

        public override void Simulate(IClient cl)
        {
            base.Simulate(cl);

            if(Input.Pressed("Attack1") && Game.IsServer)
            {
                new PortableTesterPlaced
                {
                    Position = Owner.AimRay.Position + Owner.AimRay.Forward * 34,
                    Velocity = Owner.AimRay.Forward * 180,
                    Owner = Owner
                };
                Delete();

                MyGame.Current.EventSystem.AddEventToLog(new()
                {
                    EventString = $"{cl.Name} placed a portable tester.",
                    PlayersInvolved = new() { cl.SteamId }
                });
            }
        }

        public override void SimulateAnimator(CitizenAnimationHelper anim)
        {
            base.SimulateAnimator(anim);
            anim.HoldType = CitizenAnimationHelper.HoldTypes.HoldItem;
        }
    }
}
