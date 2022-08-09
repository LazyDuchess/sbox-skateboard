using Sandbox;

namespace Skateboard.Cameras
{
	public class SkateCamera : CameraMode
	{
		private float orbitDistance = 150;
		private float orbitHeight = 50;

		private float lerpSpeed = 3f;
		public override void Update()
		{
			if ( Local.Pawn is not AnimatedEntity pawn )
				return;
			var speedLength = pawn.Velocity.WithZ(0f).Length;
			var pawnVector = pawn.Rotation.Forward;
			if (speedLength > float.Epsilon)
				pawnVector = pawn.Velocity.Normal;
			var pawnLook = Rotation.LookAt( pawnVector, Vector3.Up );
			Rotation = Rotation.Slerp( Rotation, pawnLook, lerpSpeed * Time.Delta, false );
			var center = pawn.Position + Vector3.Up * orbitHeight;
			var targetPos = center + Rotation.Backward * orbitDistance;
			var tr = Trace.Ray( center, targetPos )
					.WithAnyTags( "solid" )
					.Ignore( pawn )
					.Radius( 8 )
					.Run();

			Position = tr.EndPosition;
			/*
			Position = pawn.Position;
			Vector3 targetPos;

			var center = pawn.Position + Vector3.Up * 64;

			if ( thirdperson_orbit )
			{
				Position += Vector3.Up * ((pawn.CollisionBounds.Center.z * pawn.Scale) + orbitHeight);
				Rotation = Rotation.From( orbitAngles );

				targetPos = Position + Rotation.Backward * orbitDistance;
			}
			else
			{
				Position = center;
				Rotation = Rotation.FromAxis( Vector3.Up, 4 ) * Input.Rotation;

				float distance = 130.0f * pawn.Scale;
				targetPos = Position + Input.Rotation.Right * ((pawn.CollisionBounds.Maxs.x + 15) * pawn.Scale);
				targetPos += Input.Rotation.Forward * -distance;
			}

			if ( thirdperson_collision )
			{
				var tr = Trace.Ray( Position, targetPos )
					.WithAnyTags( "solid" )
					.Ignore( pawn )
					.Radius( 8 )
					.Run();

				Position = tr.EndPosition;
			}
			else
			{
				Position = targetPos;
			}

			FieldOfView = 70;

			Viewer = null;*/
		}

		public override void BuildInput( InputBuilder input )
		{
			/*
			if ( thirdperson_orbit && input.Down( InputButton.Walk ) )
			{
				if ( input.Down( InputButton.PrimaryAttack ) )
				{
					orbitDistance += input.AnalogLook.pitch;
					orbitDistance = orbitDistance.Clamp( 0, 1000 );
				}
				else if ( input.Down( InputButton.SecondaryAttack ) )
				{
					orbitHeight += input.AnalogLook.pitch;
					orbitHeight = orbitHeight.Clamp( -1000, 1000 );
				}
				else
				{
					orbitAngles.yaw += input.AnalogLook.yaw;
					orbitAngles.pitch += input.AnalogLook.pitch;
					orbitAngles = orbitAngles.Normal;
					orbitAngles.pitch = orbitAngles.pitch.Clamp( -89, 89 );
				}

				input.AnalogLook = Angles.Zero;

				input.Clear();
				input.StopProcessing = true;
			}
			*/
			base.BuildInput( input );
		}
	}
}
