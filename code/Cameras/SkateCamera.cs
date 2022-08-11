using Sandbox;
using Skateboard.Player;

namespace Skateboard.Cameras
{
	public class SkateCamera : CameraMode
	{
		private float orbitDistance = 170;
		private float orbitHeight = 70;

		private float rotationLerpSpeed = 3f;
		private float speedLerpSpeed = 5f;
		private float speedDistanceMultiplier = 0.05f;

		float currentSpeedDistance = 0f;

		float currentVertLerp = 0f;

		float vertCameraHeight = 200f;

		float vertCameraTall = 50f;

		float vertLerpSpeed = 1f;

		float vertRotationSpeed = 10f;

		public SkateCamera()
		{
			FieldOfView = 70f;
		}

		public override void Update()
		{
			if ( Local.Pawn is not SkatePawn pawn )
				return;
			var speedLength = pawn.Velocity.WithZ(0f).Length;
			var pawnVector = pawn.Rotation.Forward;
			var zMultiplier = pawn.Velocity.z * 0.1f;
			if ( speedLength > SkateController.StoppedVelocity )
				pawnVector = (pawn.Velocity.WithZ( 0f ) + zMultiplier).Normal;
			var pawnLook = Rotation.LookAt( pawnVector, Vector3.Up );
			
			var center = pawn.Position + pawn.Rotation.Up * orbitHeight;
			var targetPos = center + Rotation.Backward * orbitDistance;
			var targetLerpDistance = speedLength * speedDistanceMultiplier;
			currentSpeedDistance = MathX.Lerp( currentSpeedDistance, targetLerpDistance, speedLerpSpeed * Time.Delta, false );
			targetPos += Rotation.Backward * currentSpeedDistance;
			var vertCamPos = pawn.Position + (pawn.Rotation.Up * vertCameraTall) + (Vector3.Up * vertCameraHeight);
			var vertCamCenter = pawn.Position + (Vector3.Up * vertCameraHeight);
			if ( pawn.OnVert )
			{
				currentVertLerp = MathX.Lerp( currentVertLerp, 1f, vertLerpSpeed * Time.Delta, false );
				
			}
			else
				currentVertLerp = MathX.Lerp( currentVertLerp, 0f, speedLerpSpeed * Time.Delta, false );
			

			targetPos = Vector3.Lerp( targetPos, vertCamPos, currentVertLerp );
			center = Vector3.Lerp( center, vertCamCenter, currentVertLerp );

			
			var rotLerpSpeed = rotationLerpSpeed;
			
			
			var tr = Trace.Ray( center, targetPos )
					.WithAnyTags( "solid" )
					.Ignore( pawn )
					.Radius( 8 )
					.Run();



			Position = tr.EndPosition;

			if ( pawn.OnVert )
			{
				rotLerpSpeed = vertRotationSpeed;
				pawnLook = Rotation.LookAt( (pawn.Position - Position + (pawn.Rotation.Up * vertCameraTall)).Normal, pawn.VertNormal );
			}

			Rotation = Rotation.Slerp( Rotation, pawnLook, rotLerpSpeed * Time.Delta, false );
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
