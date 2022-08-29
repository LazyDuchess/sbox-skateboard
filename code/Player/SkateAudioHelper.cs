using Sandbox;
using System;

namespace Skateboard.Player
{
	public class SkateAudioHelper
	{
		Sound RollSound;
		bool PreviouslyOnGround = false;
		public void FrameSimulate( Entity pawn )
		{
			if ( pawn is not SkatePawn skatePawn )
				return;
			using ( Prediction.Off() )
			{
				var pawnVelocity = pawn.Velocity.Length;
				if ( pawn.GroundEntity == null || skatePawn.bailed)
				{
					RollSound.Stop();
					PreviouslyOnGround = false;
				}
				else
				{
					if ( !PreviouslyOnGround )
					{
						RollSound.Stop();
						RollSound = pawn.PlaySound( "skate_roll" );
					}
					RollSound.SetVolume( pawnVelocity * 0.003f );
					RollSound.SetPitch( Math.Min(1.5f, Math.Max(0.5f, pawnVelocity * 0.0009f)) );
					PreviouslyOnGround = true;
				}
			}
		}
	}
}
