using Sandbox;
using TerrorTown;

namespace end360.TTT
{
    public class PortableTesterInspectionComponent : SimulatedComponent, ISingletonComponent
    {
        public PortableTesterPlaced? LookingAt { get; protected set; }

        [Event("Player.PostSpawn")]
        static void PostSpawn(TerrorTown.Player ply) => ply.Components.Create<PortableTesterInspectionComponent>();

        /// <summary>
        /// On the server, this does nothing. On a client it will check if there is a portable tester being looked at, and if there is it will set the LookingAt value to the tester.
        /// </summary>
        /// <param name="cl"></param>
        public override void Simulate(IClient cl)
        {
            if (!Game.IsClient) return;

            var tr = Trace.Ray(Entity.AimRay, 512).WithAnyTags("solid").Run();

            if (tr.Hit && tr.Entity is PortableTesterPlaced tester)
                LookingAt = tester;
            else
                LookingAt = null;
        }
    }
}
