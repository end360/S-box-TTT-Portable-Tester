using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrorTown;

namespace end360.TTT
{
    public class PortableTesterInspectionComponent : SimulatedComponent, ISingletonComponent
    {
        public PortableTesterPlaced? LookingAt { get; protected set; }
        public override void Simulate(IClient cl)
        {
            if (!Game.IsClient) return;

            var tr = Trace.Ray(Entity.AimRay, 65565).WithAnyTags("solid").Run();

            if (tr.Hit && tr.Entity is PortableTesterPlaced tester)
                LookingAt = tester;
            else
                LookingAt = null;
        }

        [Event("Player.PostSpawn")]
        public static void PostSpawn(TerrorTown.Player ply) => ply.Components.Create<PortableTesterInspectionComponent>();
    }
}
