using SandboxEditor;
using Sandbox;
using System.Linq;

namespace Skateboard.Entities
{
	[Library( "grind_path" )]
	[HammerEntity]
	[Path( "grind_path_node", true )]
	[Title( "Grind Path" ), Category( "Gameplay" ), Icon( "moving" )]
	public class GrindPathEntity : GenericPathEntity
	{
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

		}
		public override void DrawPath( int segments, bool drawTangents = false )
		{
			base.DrawPath( segments, drawTangents );
			for ( int i = 0; i < PathNodes.Count-1; i++ ) // Starting from i = 1 because i = 0 is start.Position
			{
				DebugOverlay.Line( Transform.PointToWorld(PathNodes[i].Position), Transform.PointToWorld( PathNodes[i+1].Position), Color.Green.Darken( 0.5f ) );
			}
		}

		/// <summary>
		/// Whether the path is looped or not.
		/// </summary>
		[Property]
		public bool Looped { get; set; } = false;
	}

	[Library( "grind_path_node" )]
	public class GrindPathNodeEntity : BasePathNodeEntity
	{
	}
}
