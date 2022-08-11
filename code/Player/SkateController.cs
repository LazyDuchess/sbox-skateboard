using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Skateboard.Physics;
using Skateboard.Utils;

namespace Skateboard.Player;


/// <summary>
/// Pro Skater physics and movement controller
/// </summary>
public partial class SkateController : BasePlayerController
{
	[ConVar.Replicated]
	public static bool skate_debug { get; set; } = false;
	public static float StoppedVelocity { get; set; } = 0.01f;
	[Net] public float BodyGirth { get; set; } = 8f;
	[Net] public float BodyGirthAir { get; set; } = 2f;
	[Net] public float EyeHeight { get; set; } = 64.0f;
	[Net] public float Gravity { get; set; } = 500.0f;
	[Net, Predicted] public bool Pushing { get; set; } = false;
	[Net] public float SteerSpeed { get; set; } = 150f;
	[Net] public float BrakeForce { get; set; } = 350f;
	[Net] public float TractionForce { get; set; } = 0f;
	[Net] public float SmoothingSpeed { get; set; } = 10f;
	[Net] public float MaxStandableAngle { get; set; } = 60f;
	[Net] public float BrakeSteerMultiplier { get; set; } = 1.5f;

	//Air controls
	[Net] public float AirSpinSpeed { get; set; } = 300f;
	[Net] public float AirPitchSpeed { get; set; } = 50f;

	//Landing too sideways should bail us.
	[Net] public float SidewaysBailSpeed { get; set; } = 400f;

	//Landing on ground flat on our faces should bail us.
	[Net] public float LandBailMaxAngle { get; set; } = 60f;

	//Normal speed and acceleration
	[Net] public float Acceleration { get; set; } = 300f;
	[Net] public float PushMaxSpeed { get; set; } = 350f;

	//Speed and acceleration when crouched (jump ready)
	[Net] public float CrouchAcceleration { get; set; } = 400f;
	[Net] public float CrouchMaxSpeed { get; set; } = 500f;

	//Jump height
	[Net] public float JumpForce { get; set; } = 200f;

	//Crouching?
	[Net, Predicted] public bool JumpReady { get; set; } = false;

	//Extra time for jumping when falling off ledges
	[Net] public float CoyoteTime { get; set; } = 0.4f;
	[Net, Predicted] float currentCoyoteTime { get; set; } = 0f;

	//Jumped already, no more coyote time
	[Net, Predicted] bool jumped { get; set; } = false;

	//Minimum jump strength, speed to get to 1.0f (max) when holding crouch.
	[Net] public float JumpStrengthMinimum { get; set; } = 0.75f;
	[Net] public float JumpStrengthSpeed { get; set; } = 0.3f;

	//Current jump strength from holding ollie button
	[Net, Predicted] float currentJumpStrength { get; set; } = 0f;

	//Make floor trace when upside down way longer so that we don't sink into the ground as much if we hit the ground.
	[Net] float upsideDownDotThreshold { get; set; } = 0.5f;
	[Net] float upsideDownTraceOffset { get; set; } = 80f;

	//hack to avoid hitting ledges on verts and stuff
	[Net] float rotationTraceOffset { get; set; } = 2f;

	//Ground entity, or last ground entity, is a vert.
	[Net, Predicted] bool GroundVert { get; set; } = false;

	//Currently in the air, attached to vert?
	[Net, Predicted] bool OnVert { get; set; } = false;
	[Net, Predicted] Vector3 VertNormal { get; set; } = Vector3.Zero;

	// Transitioning out of vert
	[Net, Predicted] bool AiringOut { get; set; } = false;
	[Net] float AirOutSpeed { get; set; } = 2.5f;

	// Collision hack
	[Net] float wallCollisionSphereRadius { get; set; } = 5f;
	[Net] float wallCollisionSphereHeight { get; set; } = 15f;
	[Net] float wallCollisionSphereGroundDistance { get; set; } = 5f;
	[Net] float wallCollisionSphereForce { get; set; } = 5f;

	public virtual void UpdateGroundEntity( TraceResult tr )
	{
		OnVert = false;
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
		if (GroundVert)
		{
			GroundVert = false;
			OnVert = true;
			VertNormal = new Vector3( GroundNormal.x, GroundNormal.y, 0f ).Normal;
			(Pawn as SkatePawn).VertNormal = VertNormal;
			Velocity -= Velocity.ProjectOnNormal( VertNormal );
			Position += VertNormal * 2.5f;
			if ( InputLD.ForwardDown )
			{
				var speedTransfer = 0.75f;
				var upVel = Math.Max( 0f, Velocity.z * speedTransfer );
				Velocity -= upVel * Vector3.Up;
				Velocity -= VertNormal * upVel;
				Position += Vector3.Up * 5f;
				AirOut(true);
			}
		}
		GroundEntity = null;
		GroundNormal = Vector3.Up;
	}

	

	bool CanJump()
	{
		if ( GroundEntity != null)
			return true;
		if ( currentCoyoteTime > 0f && jumped == false )
			return true;
		return false;
	}

	void AirOut(bool force = false)
	{
		var shouldAirOut = false;
		if ( GroundEntity == null )
			shouldAirOut = true;
		if ( OnVert == true && Velocity.Dot( Vector3.Up ) <= 0f )
			shouldAirOut = false;
		shouldAirOut = force ? true : shouldAirOut;
		if ( shouldAirOut )
		{
			var skatePawn = Pawn as SkatePawn;
			if ( OnVert )
				Velocity -= VertNormal * 25f;
			AiringOut = true;
			OnVert = false;
			skatePawn.OnVert = false;
		}
	}

	public override void Simulate()
	{
		/*
		if ( broken )
			return;*/
		var skatePawn = Pawn as SkatePawn;
		skatePawn.OnVert = OnVert;
		var RealRotation = skatePawn.RealRotation;
		var stopped = Velocity.Length <= StoppedVelocity;
		var jump = false;
		if ( Input.Down( InputButton.Jump ) )
			JumpReady = true;
		else
		{
			if ( JumpReady && CanJump() )
				jump = true;
			else
				currentJumpStrength = JumpStrengthMinimum;
			JumpReady = false;
		}
		if ( stopped )
		{
			if ( InputLD.ForwardDown )
				Pushing = true;
		}
		else
		{
			if ( GroundEntity != null )
				Pushing = !InputLD.BackDown;
		}
		var hardTurn = false;
		var braking = InputLD.BackDown;
		if ( braking )
			Pushing = false;
		if ( JumpReady && !braking )
			Pushing = true;
		var steering = InputLD.DigitalLeftInput != 0f;
		if ( steering && braking )
		{
			braking = false;
			hardTurn = true;
		}
		//Rotation *= Rotation.RotateAroundAxis( Vector3.Up, 0.1f/*Input.Left * SteerSpeed * Time.Delta*/);
		EyeRotation = RealRotation;

		//RealRotation = (RealRotation.Angles() + (AngularVelocity * Time.Delta)).ToRotation();
		RealRotation = RealRotation.RotateAroundAxis( new Vector3( 0, 1, 0 ), AngularVelocity.pitch * Time.Delta );
		RealRotation = RealRotation.RotateAroundAxis( new Vector3( 0, 1, 0 ), AngularVelocity.roll * Time.Delta );
		RealRotation = RealRotation.RotateAroundAxis( new Vector3( 0, 0, 1 ), AngularVelocity.yaw * Time.Delta );

		Rotation = Rotation.RotateAroundAxis( new Vector3( 0, 1, 0 ), AngularVelocity.pitch * Time.Delta );
		Rotation = Rotation.RotateAroundAxis( new Vector3( 0, 1, 0 ), AngularVelocity.roll * Time.Delta );
		Rotation = Rotation.RotateAroundAxis( new Vector3( 0, 0, 1 ), AngularVelocity.yaw * Time.Delta );

		if ( GroundEntity != null )
			AiringOut = false;

		if (AiringOut && GroundEntity == null)
		{
			var desiredRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, Vector3.Up ) * RealRotation;
			RealRotation = Rotation.Slerp( RealRotation, desiredRotation, AirOutSpeed * Time.Delta );
		}

		if (Input.Pressed(InputButton.Duck))
		{
			AirOut();
		}

		//Rotation = Input.Rotation;
		var vertTraceLen = 100000f;
		var vertTraceInside = 2.5f;
		var vertInsideOffset = 0.5f;

		if ( OnVert )
		{
			var vertTrace = Trace.Ray( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen) )
							.WorldAndEntities()
							.WithAnyTags( "vert" );
			if (skate_debug)
				DebugOverlay.Line( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen), Color.Green );

			var vertResult = vertTrace.Run();
			if ( vertResult.Entity != null )
			{
				var vertNormal = (vertResult.Normal - vertResult.Normal.ProjectOnNormal( Vector3.Up )).Normal;
				if ( VertNormal != vertNormal )
				{
					var off = -RealRotation.Up * vertTraceInside;
					Position = new Vector3( vertResult.HitPosition.x, vertResult.HitPosition.y, Position.z ) + off;
					RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, vertNormal ) * RealRotation;
					Position += vertNormal * vertInsideOffset;
					VertNormal = vertNormal;
					skatePawn.VertNormal = vertNormal;
				}
			}
			Velocity -= Velocity.ProjectOnNormal( VertNormal );
		}

		if ( GroundEntity == null )
		{
			if (!OnVert)
				AngularVelocity = new Angles( (InputLD.DigitalForwardInput * AirPitchSpeed), (InputLD.DigitalLeftInput * AirSpinSpeed), 0f );
			else
				AngularVelocity = new Angles( 0f, (InputLD.DigitalLeftInput * AirSpinSpeed), 0f );
			Velocity += (Gravity * Vector3.Down) * Time.Delta;
		}
		else
		{
			AngularVelocity = new Angles( 0f, (InputLD.DigitalLeftInput * SteerSpeed * (hardTurn ? BrakeSteerMultiplier : 1f)), 0f );
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
		}
		var helperVelocity = Velocity;
		var upVelocity = Vector3.Zero;
		if ( GroundEntity == null )
		{
			upVelocity = Velocity.ProjectOnNormal( Vector3.Up );
			helperVelocity = Velocity - upVelocity;
		}
		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		var helper = new SkateHelper( Position, Velocity );
		helper.MaxStandableAngle = MaxStandableAngle;
		//helper.Trace = helper.Trace.Size( 0.5f );
		var collHeight = 15f;
		/*
		if ( GroundEntity == null )
			collHeight = 20f;*/
		var girth = BodyGirth;
		if ( GroundEntity == null )
			girth = BodyGirthAir;
		var mins = new Vector3( -girth, -girth, 0/*collHeight*/);
		var maxs = new Vector3( +girth, +girth,/* collHeight +*/ 10 );
		if ( GroundEntity != null )
		{
			mins += RealRotation.Up * collHeight;
			maxs += RealRotation.Up * collHeight;
		}
		else
		{
			mins += Vector3.Up * collHeight;
			maxs += Vector3.Up * collHeight;
		}
		helper.Trace = helper.Trace.Size( mins, maxs );

		//Debug
		//DebugOverlay.Box( Position + TraceOffset, mins, maxs, Color.Red );
		if (skate_debug)
			DebugOverlay.Box( Position, mins, maxs, Color.Blue );

		if ( helper.TryMove( Time.Delta, GroundEntity == null ) > 0)
		{
			//TODO: ignore collisions when going up walkable stuff while still preventing the player from going oob. Maybe push away when inside walls in skatehelper?
				
			if ( helper.HitWall)
			{
				Position = helper.Position;
				Velocity = helper.Velocity;
				if ( helper.HitWall && GroundEntity != null)
				{
					RealRotation = Rotation.LookAt( helper.Velocity.WithZ(0f), RealRotation.Up );
				}
			}
			else
			{
				Position = helper.Position;
				Velocity = helper.Velocity;
			}
			//Rotation = RealRotation;
		}
		else
			Position += Velocity * Time.Delta;

		Rotation = Rotation.Lerp( Rotation, RealRotation, SmoothingSpeed * Time.Delta );
		
		if ( GroundEntity != null )
		{
			var tracePos = Position + (RealRotation.Up * wallCollisionSphereHeight) + (Vector3.Up * wallCollisionSphereGroundDistance);
			var sphereTrace = Trace.Sphere( wallCollisionSphereRadius, tracePos, tracePos )
						.WorldAndEntities()
						.WithAnyTags( "solid", "playerclip", "passbullets", "unskateable" );



			var sphereResult = sphereTrace.Run();

			if ( sphereResult.Hit )
			{
					var heading = sphereResult.HitPosition - tracePos;
					heading = heading.Normal;
			if (GroundEntity != null)
			{
					heading -= heading.ProjectOnNormal( GroundNormal );
				heading = heading.Normal;
			}
					var distance = Vector3.DistanceBetween( tracePos, sphereResult.HitPosition );
					Position -= heading * (distance + wallCollisionSphereForce);
			var dot = Velocity.Dot( heading );
			if (dot < 0f)
				Velocity -= Velocity.ProjectOnNormal( heading );
					Log.Info( "sphere hit" );
			}
			if (skate_debug)
				DebugOverlay.Sphere( tracePos, wallCollisionSphereRadius, Color.Red );
		}
		var groundVector = Vector3.Up;
		if ( GroundEntity != null )
			groundVector = GroundNormal;

		var wasOnGround = GroundEntity != null;

		var currentTrace = 4f;
		
		if ( GroundEntity == null )
			currentTrace = 2f;
		var traceOffset1 = groundVector * 25f;
		var traceOffset2 = -(groundVector * currentTrace);

		if ( GroundEntity == null )
		{
			var rotationOffset = RealRotation.Up * rotationTraceOffset;
			rotationOffset.z = 0f;
			traceOffset1 = groundVector * 25f + rotationOffset;
			var upsidedownDot = Vector3.Dot( RealRotation.Up, Vector3.Down );
			if ( upsidedownDot >= upsideDownDotThreshold )
				currentTrace += upsideDownTraceOffset;
			traceOffset2 = -(groundVector * currentTrace) + rotationOffset;
		}

		var floorTrace = Trace.Ray( Position + traceOffset1, Position + traceOffset2 )
							.WorldAndEntities()
							.WithAnyTags( "solid", "playerclip", "passbullets", "player", "vert", "skateable", "unskateable" );
		if (skate_debug)
			DebugOverlay.Line( Position + traceOffset1, Position + traceOffset2, Color.Red );
		//DebugOverlay.Line( Position + groundVector * 15f, Position - groundVector * currentTrace, Color.Red );
		var floorResult = floorTrace.Run();
		if ( floorResult.Entity == null || floorResult.Tags.Contains("unskateable"))
		{
			ClearGroundEntity();
		}
		else
		{
			var isVert = false;
			if ( floorResult.Tags.Contains( "vert" ) )
				isVert = true;
			var canStand = false;
			if ( !floorResult.StartedSolid && floorResult.Fraction > 0.0f && floorResult.Fraction < 1.0f )
			{
				if ( floorResult.Normal.Angle( Vector3.Up ) < MaxStandableAngle )
					canStand = true;
				if ( floorResult.Tags.Contains( "vert" ) || floorResult.Tags.Contains("skateable"))
					canStand = true;
			}
			if ( canStand )
			{
				var oldForwardSpeed = Velocity.Dot( RealRotation.Forward );
				var prevGroundEnt = GroundEntity;
				UpdateGroundEntity( floorResult );
				var oldRotation = RealRotation;
				RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, GroundNormal ) * RealRotation;
				if ( prevGroundEnt == null )
				{
					var bailed = false;
					var angleDifference = oldRotation.Up.Normal.Angle( floorResult.Normal );
					if ( angleDifference > LandBailMaxAngle )
					{
						Log.Info( "Landed awkwardly. (Angle: " + angleDifference + ")" );
						skatePawn.Bail();
						bailed = true;
					}
					var sidewaysVelocity = Math.Abs( Vector3.Dot( Velocity, RealRotation.Right ) );
					if ( sidewaysVelocity >= SidewaysBailSpeed && !bailed )
					{
						Log.Info( "Landed sideways. (Velocity: " + sidewaysVelocity + ")" );
						skatePawn.Bail();
					}
					if ( Vector3.Dot( Velocity, RealRotation.Forward ) < 0 )
					{
						RealRotation = Rotation.LookAt( RealRotation.Backward, GroundNormal );
					}
					Rotation = RealRotation;
				}
				else
				{
					Velocity = oldForwardSpeed * RealRotation.Forward;
				}
				RealRotation = Rotation.LookAt( RealRotation.Forward, GroundNormal );
				Velocity -= Velocity.ProjectOnNormal( floorResult.Normal );
				Position = floorResult.EndPosition + floorResult.Normal * 1f;
				GroundVert = isVert;
				
			}
			else
			{
				ClearGroundEntity();
				//Velocity -= Velocity.ProjectOnNormal( floorResult.Normal );
			} 
		}
		/*
		if (GroundEntity != null)
		{*/
			
		//}
		/*
		if ( !floorResult.StartedSolid && floorResult.Fraction > 0.0f && floorResult.Fraction < 1.0f )
		{
			Position = floorResult.EndPosition + floorResult.Normal * 1f;
		}*/

		skatePawn.RealRotation = RealRotation;

		if ( currentCoyoteTime > 0f )
			currentCoyoteTime -= Time.Delta;
		else
			jumped = false;

		if (GroundEntity != null)
		{
			currentCoyoteTime = 0f;
			jumped = false;
		}

		if ( wasOnGround == true && GroundEntity == null )
			currentCoyoteTime = CoyoteTime;

		if ( jump )
		{
			ClearGroundEntity();
			Velocity += Vector3.Up * JumpForce * currentJumpStrength;
			jumped = true;
			currentJumpStrength = JumpStrengthMinimum;
		}

		if ( JumpReady )
		{
			if ( currentJumpStrength < 1f )
				currentJumpStrength += Time.Delta * JumpStrengthSpeed;
			else
				currentJumpStrength = 1f;
		}
	}

	public override void FrameSimulate()
	{
		var RealRotation = (Pawn as SkatePawn).RealRotation;
		Rotation = Rotation.Lerp( Rotation, RealRotation, SmoothingSpeed * Time.Delta );
	}
}
