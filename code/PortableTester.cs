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
                var placed = new PortableTesterPlaced();
                placed.Position = Owner.AimRay.Position + Owner.AimRay.Forward * 34;
                placed.Velocity = Owner.AimRay.Forward * 180;
                placed.Owner = Owner;
                Delete();
            }
        }

        public override void SimulateAnimator(CitizenAnimationHelper anim)
        {
            base.SimulateAnimator(anim);
            anim.HoldType = CitizenAnimationHelper.HoldTypes.HoldItem;
        }
    }
}
