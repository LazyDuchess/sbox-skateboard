using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Skateboard
{
	public class SkateAnimator : PawnAnimator
	{
		public SkateAnimator()
		{

		}
		public SkateAnimator(Entity pawn) : this()
		{
			this.Pawn = pawn;
		}
		public override void Simulate()
		{
			var player = Pawn as SkatePawn;
			if ( player == null )
				return;
			SetAnimParameter( "b_onground", player.GroundEntity != null );
			SetAnimParameter( "b_crouch", player.Crouch );
			SetAnimParameter( "f_left", -player.TurnRight );
		}
		public override void OnEvent( string name )
		{
			switch(name)
			{
				case "jump":
					Trigger( "b_ollie" );
					break;
				case "land":
					Trigger( "b_land" );
					break;
				case "front":
					Trigger( "b_hit_front" );
					break;
				case "back":
					Trigger( "b_hit_back" );
					break;
				case "angry":
					Trigger( "b_angy" );
					break;
			}
			base.OnEvent( name );
		}
	}
}
