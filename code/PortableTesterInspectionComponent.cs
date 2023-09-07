using Sandbox;
using TerrorTown;

namespace end360.TTT
{
    public class PortableTesterInspectionComponent : SimulatedComponent, ISingletonComponent
    {
        public PortableTesterPlaced? LookingAt { get; protected set; }

        [Event("Player.PostSpawn")]
        static void PostSpawn(TerrorTown.Player ply) => ply.Components.Create<PortableTesterInspectionComponent>();

        public override void Simulate(IClient cl)
        {
            if (!Game.IsClient) return;

            var tr = Trace.Ray(Entity.AimRay, 65565).WithAnyTags("solid").Run();

            if (tr.Hit && tr.Entity is PortableTesterPlaced tester)
                LookingAt = tester;
            else
                LookingAt = null;
        }
    }
}
