using Sandbox;
using Skateboard.Player;

namespace Skateboard.Cameras
{
	public class FPSkateCamera : CameraMode
	{
		//Terry cam
		
		private float orbitDistance = 170f;
		private float orbitHeight = 70f;
		float vertCameraHeight = 200f;
		float vertCameraTall = 50f;
		float fov = 90f;
		float lookDown = 0f;
		float minimumDistance = 0.2f;

		//Skateboard cam
		/*
		private float orbitDistance = 150f;
		private float orbitHeight = 40f;
		float vertCameraHeight = 200f;
		float vertCameraTall = 50f;
		float fov = 55f;
		float lookDown = 10f;*/

		private float rotationLerpSpeed = 3f;
		private float speedLerpSpeed = 5f;
		private float speedDistanceMultiplier = 0.05f;

		float currentSpeedDistance = 0f;

		float currentVertLerp = 0f;

		float vertLerpSpeed = 1f;

		float vertRotationSpeed = 10f;

		float targetTowardsOriginOffset = 1f;

		public FPSkateCamera()
		{
			FieldOfView = fov;
			ZNear = 0.1f;
		}

		public override void Update()
		{
			if ( Local.Pawn is not SkatePawn pawn )
				return;
			if (pawn.bailed)
			{
				pawn.bailedEntity.EnableDrawing = false;
				Position = pawn.bailedEntity.GetBoneTransform( pawn.GetBoneIndex( "head" ), true ).Position;
				Rotation = Rotation.LookAt(pawn.bailedEntity.GetBoneTransform( pawn.GetBoneIndex( "head" ), true ).Rotation.Forward, Vector3.Up);
				return;
			}
			pawn.EnableDrawing = false;
			Position = pawn.GetBoneTransform( pawn.GetBoneIndex( "head" ), true ).Position;
			Rotation = pawn.Rotation;
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
