using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Skateboard.Physics;

namespace Skateboard.Player;


/// <summary>
/// Pro Skater physics and movement controller
/// </summary>
public partial class SkateController : BasePlayerController
{
	[Net] public float BodyWidth { get; set; } = 8f;
	[Net] public float BodyLength { get; set; } =8f;
	[Net] public float EyeHeight { get; set; } = 64.0f;
	[Net] public float Gravity { get; set; } = 500.0f;
	[Net] public bool Pushing { get; set; } = true;
	[Net] public float SteerSpeed { get; set; } = 150f;
	[Net] public float BrakeForce { get; set; } = 350f;
	[Net] public float TractionForce { get; set; } = 0f;
	[Net] public float SmoothingSpeed { get; set; } = 20f;
	[Net] public float MaxStandableAngle { get; set; } = 60f;
	[Net] public float BrakeSteerMultiplier { get; set; } = 1.5f;
	[Net] public float AirSpinSpeed { get; set; } = 300f;
	[Net] public float SidewaysBailSpeed { get; set; } = 250f;
	[Net] public float LandBailMaxAngle { get; set; } = 50f;

	[Net] public float Acceleration { get; set; } = 200f;
	[Net] public float PushMaxSpeed { get; set; } = 300f;

	[Net] public float CrouchAcceleration { get; set; } = 300f;
	[Net] public float CrouchMaxSpeed { get; set; } = 400f;

	[Net] public float JumpForce { get; set; } = 200f;

	[Net, Predicted] public bool JumpReady { get; set; } = false;

	[Net] bool broken { get; set; } = false;

	public virtual void UpdateGroundEntity( TraceResult tr )
	{
		GroundNormal = tr.Normal;

		GroundEntity = tr.Entity;

		if ( GroundEntity != null )
		{
			BaseVelocity = GroundEntity.Velocity;
		}
	}

	/// <summary>
	/// We're no longer on the ground, remove it
	/// </summary>
	public virtual void ClearGroundEntity()
	{
		if ( GroundEntity == null ) return;

		GroundEntity = null;
		GroundNormal = Vector3.Up;
	}

	//I'm a unity slut
	//TODO: Clean up ffs
	Rotation FromToRotation( Vector3 fromDirection, Vector3 toDirection )
	{
		Vector3 axis = Vector3.Cross( fromDirection, toDirection );
		float angle = Vector3.GetAngle( fromDirection, toDirection );
		if ( angle >= 179.9196f )
		{
			var r = Vector3.Cross( fromDirection, Vector3.Right );
			axis = Vector3.Cross( r, fromDirection );
			if ( axis.LengthSquared < 0.000001f )
				axis = Vector3.Up;
		}
		return AngleAxis( angle, axis.Normal );
		//return RotateTowards( Rotation.LookAt( fromDirection ), Rotation.LookAt( toDirection ), float.MaxValue );
	}

	public static Rotation AngleAxis( float aAngle, Vector3 aAxis )
	{
		aAxis = aAxis.Normal;
		float rad = aAngle * (float)(Math.PI / 180.0) * 0.5f;
		aAxis *= (float)Math.Sin( rad );
		return new Rotation( aAxis.x, aAxis.y, aAxis.z, (float)Math.Cos( rad ) );
	}

	public override void Simulate()
	{
		/*
		if ( broken )
			return;*/
		var skatePawn = Pawn as SkatePawn;
		var RealRotation = skatePawn.RealRotation;
		var stopped = Velocity.Length <= float.Epsilon;
		var jump = false;
		if ( Input.Down( InputButton.Jump ) )
			JumpReady = true;
		else
		{
			if ( JumpReady && GroundEntity != null )
				jump = true;
			JumpReady = false;
		}
		if ( stopped )
		{
			if (Input.Down( InputButton.Forward ))
				Pushing = true;
		}
		else
		{
			Pushing = !Input.Down( InputButton.Back );
		}
		var hardTurn = false;
		var braking = Input.Down( InputButton.Back );
		if ( braking )
			Pushing = false;
		if ( JumpReady && !braking )
			Pushing = true;
		var steering = Input.Left != 0f;
		if ( steering && braking )
		{
			braking = false;
			hardTurn = true;
		}

		//Rotation *= Rotation.RotateAroundAxis( Vector3.Up, 0.1f/*Input.Left * SteerSpeed * Time.Delta*/);
		EyeRotation = RealRotation;

		RealRotation = (RealRotation.Angles() + (AngularVelocity * Time.Delta)).ToRotation();
		//Rotation = Input.Rotation;


		if ( GroundEntity == null )
		{
			AngularVelocity = new Angles( 0f, (Input.Left * AirSpinSpeed), 0f );
			Velocity += (Gravity * Vector3.Down) * Time.Delta;
		}
		else
		{
			AngularVelocity = new Angles( 0f, (Input.Left * SteerSpeed * (hardTurn ? BrakeSteerMultiplier : 1f)), 0f );
			//Log.Info( "On ground" );
			Velocity -= Velocity.ProjectOnNormal( GroundNormal );
			var forwardOnlyVelocity = Velocity.ProjectOnNormal( RealRotation.Forward );
			var sideOnlyVelocity = Velocity.ProjectOnNormal( RealRotation.Right );
			var leftToTraction = sideOnlyVelocity.Length;
			{
				var traction = TractionForce * Time.Delta;
				if ( traction > leftToTraction )
					traction = leftToTraction;
				sideOnlyVelocity -= traction * sideOnlyVelocity.Normal;
			}
			//100% traction for now, might be like that in thps.
			Velocity = forwardOnlyVelocity;
			if ( Pushing )
			{
				var pushAccel = Acceleration;
				var pushSpeed = PushMaxSpeed;
				if ( JumpReady )
				{
					pushAccel = CrouchAcceleration;
					pushSpeed = CrouchMaxSpeed;
				}
				if ( Velocity.Length < pushSpeed )
				{
					Velocity += Vector3.Forward * RealRotation * pushAccel * Time.Delta;
					if ( Velocity.Length > pushSpeed )
					{
						Velocity = Velocity.Normal * pushSpeed;
					}
				}
			}
			else
			{
				if ( braking )
				{
					var leftToStop = Velocity.Length;
					if ( leftToStop > 0f )
					{
						var deaccel = BrakeForce * Time.Delta;
						if ( deaccel > leftToStop )
							deaccel = leftToStop;
						Velocity -= deaccel * Velocity.Normal;
					}
				}
			}
			if (jump)
			{
				ClearGroundEntity();
				Velocity += Vector3.Up * JumpForce;
			}
		}
		var helperVelocity = Velocity;
		var upVelocity = Vector3.Zero;
		if (GroundEntity == null)
		{
			upVelocity = Velocity.ProjectOnNormal( Vector3.Up );
			helperVelocity = Velocity - upVelocity;
		}
		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		var helper = new SkateHelper( Position, Velocity );
		helper.MaxStandableAngle = MaxStandableAngle;
		//helper.Trace = helper.Trace.Size( 0.5f );
		var collHeight = 10f;
		/*
		if ( GroundEntity == null )
			collHeight = 20f;*/
		var mins = new Vector3( -BodyWidth, -BodyLength, collHeight );
		var maxs = new Vector3( +BodyWidth, +BodyLength, collHeight );
		helper.Trace = helper.Trace.Size( mins, maxs );

		//Debug
		DebugOverlay.Box( Position + TraceOffset, mins, maxs, Color.Red );
		DebugOverlay.Box( Position, mins, maxs, Color.Blue );

		if ( helper.TryMove( Time.Delta ) > 0)
		{
			//TODO: ignore collisions when going up walkable stuff while still preventing the player from going oob. Maybe push away when inside walls in skatehelper?
			/*
			if ( helper.HitWall)
			{*/
				Position = helper.Position;
				Velocity = helper.Velocity;
			if ( helper.HitWall )
			{
				RealRotation = Rotation.LookAt( helper.Velocity.WithZ(0f), RealRotation.Up );
			}
			/*}
			else
			{
				Position += Velocity * Time.Delta;
			}*/
			//Rotation = RealRotation;
		}
		else
			Position += Velocity * Time.Delta;
		if ( GroundEntity != null)
		{
			Rotation = Rotation.Lerp( Rotation, RealRotation, SmoothingSpeed * Time.Delta );
		}
		else
		{
			//Sync rotation with visual rotation if we are not snapping to ground normals
			Rotation = RealRotation;
		}

		var groundVector = Vector3.Up;
		if ( GroundEntity != null )
			groundVector = GroundNormal;

		var floorTrace = Trace.Ray( Position + groundVector * 30f, Position - groundVector * 4f )
							.WorldAndEntities()
							.WithAnyTags( "solid", "playerclip", "passbullets", "player" );


		DebugOverlay.Line( Position + groundVector * 30f, Position - groundVector * 4f , Color.Red );

		var floorResult = floorTrace.Run();
		if ( floorResult.Entity == null )
		{
			ClearGroundEntity();
		}
		else
		{
			if ( floorResult.Normal.Angle( Vector3.Up ) < MaxStandableAngle )
			{
				var prevGroundEnt = GroundEntity;
				UpdateGroundEntity( floorResult );
				var oldRotation = RealRotation;
				RealRotation = FromToRotation( Vector3.Up * RealRotation, GroundNormal ) * RealRotation;
				if ( prevGroundEnt == null )
				{
					if ( oldRotation.Up.Normal.Angle( floorResult.Normal ) > LandBailMaxAngle )
						skatePawn.Bail();
					var sidewaysVelocity = Math.Abs( Vector3.Dot( Velocity, RealRotation.Right ) );
					if ( sidewaysVelocity >= SidewaysBailSpeed )
						skatePawn.Bail();
					if ( Vector3.Dot( Velocity, RealRotation.Forward ) < 0 )
					{
						RealRotation = Rotation.LookAt( RealRotation.Backward, GroundNormal );
					}
					Rotation = RealRotation;
				}
				RealRotation = Rotation.LookAt( RealRotation.Forward, GroundNormal );
				Velocity -= Velocity.ProjectOnNormal( floorResult.Normal );
			}
			else
			{
				ClearGroundEntity();
				var dotVelocity = Vector3.Dot( Velocity, -floorResult.Normal );
				if ( dotVelocity > 0 )
					Velocity -= Velocity.ProjectOnNormal( -floorResult.Normal );
			}
		}
		if ( !floorResult.StartedSolid && floorResult.Fraction > 0.0f && floorResult.Fraction < 1.0f )
		{
			Position = floorResult.EndPosition + floorResult.Normal * 1f;
		}
		skatePawn.RealRotation = RealRotation;
	}

	public override void FrameSimulate()
	{
		// Update rotation every frame, to keep things smooth
		//Rotation = Input.Rotation;
		//EyeRotation = Rotation;
	}
}
