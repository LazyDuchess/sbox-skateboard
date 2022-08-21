using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Skateboard.Physics;
using Skateboard.Tricks;
using Skateboard.Utils;

namespace Skateboard.Player;


/// <summary>
/// Pro Skater physics and movement controller
/// </summary>
public partial class SkateController : BasePlayerController
{
	[Net] public float AirTopSpeed { get; set; } = 2000f;
	[Net] public float TopSpeed { get; set; } = 1300f;
	//Max slope difference when on ground
	[Net] public float MaxSlope { get; set; } = 75f;
	//For vert
	[Net, Predicted] public float NudgeAmount { get; set; } = 0f;
	//ollie spin trick stuff
	[Net, Predicted] public float SpunAmount { get; set; } = 0f;
	[Net] public float LandSpeedMultiplier { get; set; } = 0.0f;
	//Vert under us? To allow leveling out while falling.
	[Net, Predicted] public bool HasVertBelow { get; set; } = false;
	//Speed at which hitting a wall cancels your combo
	[Net, Predicted] public float HitForce { get; set; } = 200f;
	//Clientside
	public bool SnapRotation { get; set; } = false;

	[ConVar.Replicated]
	public static bool skate_debug { get; set; } = false;
	public static float StoppedVelocity { get; set; } = 0.01f;
	[Net] public float BodyGirth { get; set; } = 8f;
	[Net] public float BodyGirthAir { get; set; } = 2f;
	[Net] public float EyeHeight { get; set; } = 64.0f;
	[Net] public float Gravity { get; set; } = 800.0f;
	[Net, Predicted] public bool Pushing { get; set; } = false;
	[Net] public float SteerSpeed { get; set; } = 150f;
	[Net] public float BrakeForce { get; set; } = 450f;
	[Net] public float TractionForce { get; set; } = 0f;
	[Net] public float SmoothingSpeed { get; set; } = 10f;
	[Net] public float MaxStandableAngle { get; set; } = 70f;
	[Net] public float MinFallableAngle { get; set; } = 70f;
	[Net] public float BrakeSteerMultiplier { get; set; } = 1.5f;

	//Air controls
	[Net] public float AirSpinSpeed { get; set; } = 325f;
	[Net] public float AirPitchSpeed { get; set; } = 50f;

	//Landing too sideways should bail us.
	[Net] public float SidewaysBailSpeed { get; set; } = 400f;

	//Landing on ground flat on our faces should bail us.
	[Net] public float LandBailMaxAngle { get; set; } = 60f;

	//Normal speed and acceleration
	[Net] public float Acceleration { get; set; } = 400f;
	[Net] public float PushMaxSpeed { get; set; } = 450f;

	//Speed and acceleration when crouched (jump ready)
	[Net] public float CrouchAcceleration { get; set; } = 600f;
	[Net] public float CrouchMaxSpeed { get; set; } = 700f;

	//Jump height
	[Net] public float JumpForce { get; set; } = 250f;

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
	[Net] float upsideDownDotThreshold { get; set; } = 0.1f;
	[Net] float upsideDownTraceOffset { get; set; } = 70f;

	//hack to avoid hitting ledges on verts and stuff
	[Net] float rotationTraceOffset { get; set; } = 2f;

	//Ground entity, or last ground entity, is a vert.
	[Net, Predicted] bool GroundVert { get; set; } = false;

	//Currently in the air, attached to vert?
	[Net, Predicted] bool OnVert { get; set; } = false;
	[Net, Predicted] Vector3 VertNormal { get; set; } = Vector3.Zero;

	// Transitioning out of vert
	[Net, Predicted] bool AiringOut { get; set; } = false;
	[Net] float AirOutSpeed { get; set; } = 2f;

	// Collision hack
	[Net] float wallCollisionSphereRadius { get; set; } = 6f;
	[Net] float wallCollisionSphereHeight { get; set; } = 15f;
	[Net] float wallCollisionSphereGroundDistance { get; set; } = 5f;
	[Net] float wallCollisionSphereForce { get; set; } = 10f;

	public SkateController()
	{
		if ( Game.skate_sim_mode )
			SetSimStats();
		
	}

	public SkateController(Entity pawn) : this()
	{
		Pawn = pawn;
	}
	void SetSimStats()
	{
		Gravity = 850f;
		CrouchMaxSpeed = 600f;
		JumpForce = 240f;
		CoyoteTime = 0.1f;
		SteerSpeed = 125f;
		BrakeSteerMultiplier = 1.25f;
		SidewaysBailSpeed = 320f;
		LandSpeedMultiplier = 0.0f;
		LandBailMaxAngle = 50f;
		AirOutSpeed = 1f;
		TopSpeed = 1000f;
	}
	void CalculateSpinTrick()
	{
		if ( GroundEntity != null )
			return;
		SpunAmount += AngularVelocity.yaw * Time.Delta;
	}

	void FinishSpinTrick()
	{
		var finalAmount = Math.Ceiling( Math.Abs(SpunAmount) / 180f) * 180f;
		var remainder = Math.Abs( SpunAmount ) % 180f;
		if ( remainder <= 90 )
			finalAmount -= 180;
		if ( finalAmount <= 0 )
			return;
		//Debug( (SpunAmount / 180f).ToString() );
		var trickSide = "BS";
		if ( SpunAmount < 0f )
			trickSide = "FS";
		var finalTrickName = trickSide + " " + finalAmount.ToString() + " Ollie";
		var spins = (finalAmount / 180f) - 1;
		var trick = new TrickScoreEntry( finalTrickName, (int)(100 + (spins * 50)));
		var skatePawn = Pawn as SkatePawn;
		skatePawn.TrickScores.Add( trick );
	}

	public virtual void UpdateGroundEntity( TraceResult tr )
	{
		FinishSpinTrick();
		SpunAmount = 0f;
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
	public virtual void ClearGroundEntity(ref Rotation RealRotation)
	{
		if ( GroundEntity == null ) return;
		NudgeAmount = 0f;
		if (GroundVert)
		{
			GroundVert = false;
			OnVert = true;
			VertNormal = new Vector3( GroundNormal.x, GroundNormal.y, 0f ).Normal;
			(Pawn as SkatePawn).VertNormal = VertNormal;
			Velocity -= Velocity.ProjectOnNormal( VertNormal );
			Position += VertNormal * 1.5f;
			if ( InputLD.ForwardDown )
			{
				var speedTransfer = 0.75f;
				var upVel = Math.Max( 0f, Velocity.z * speedTransfer );
				Velocity -= upVel * Vector3.Up;
				Velocity -= VertNormal * upVel;
				Position += Vector3.Up * 5f;
				AirOut( true );
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
		if ( OnVert == true && !HasVertBelow )
			shouldAirOut = true;
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

	public override void CleanUp()
	{
		if ( Pawn is not SkatePawn skatePawn )
			return;
		skatePawn.RealRotation = Rotation;
		var pawnRotation = skatePawn.RealRotation;
		ClearGroundEntity(ref pawnRotation);
		OnVert = false;
		HasVertBelow = false;
		Pushing = false;
		skatePawn.RealRotation = skatePawn.Rotation;
		skatePawn.Velocity = Vector3.Zero;
		skatePawn.AngularVelocity = Angles.Zero;
		SpunAmount = 0f;
	}

	void DoUpsideDownFall(ref Rotation RealRotation, ref bool isFalling)
	{
		isFalling = false;
		if ( GroundEntity == null )
			return;
		var angle = GroundNormal.Angle(Vector3.Up);
		if ( angle < MinFallableAngle )
			return;
		var requiredSpeed = angle * 5f;
		var vel = Vector3.Dot(RealRotation.Forward,Velocity);
		var downRotationSpeed = angle * 0.025f;
		var downForwardSpeed = angle * 2f;
		var downAngle = Rotation.LookAt( Vector3.Down, GroundNormal );
		var downRotation = MathLD.FromToRotation( Vector3.Up * downAngle, GroundNormal ) * downAngle;
		var downwardFacing = RealRotation.Forward.Dot( downRotation.Forward );
		//Give us less speed if we're trying to go upwards.
		if ( downwardFacing <= 0f )
			downForwardSpeed = angle * 2f;
		//Not a fan of losing speed like this on vert tbh. Leave it for loops, non vert steep slopes and if we're too slow on vert.
		/*
		if ( GroundVert && vel >= requiredSpeed )
			return;*/
		Velocity += downRotation.Forward * downForwardSpeed * Time.Delta;
		if ( vel >= requiredSpeed )
			return;
		isFalling = true;
		RealRotation = Rotation.Lerp( RealRotation, downRotation, downRotationSpeed * Time.Delta );
		if ( angle > 90f )
		{
			
			requiredSpeed = angle * 4f;
			Debug( requiredSpeed );
			if ( vel < requiredSpeed )
			{
				Position += GroundNormal * 20f;
				ClearGroundEntity(ref RealRotation);
				Debug( "you fell off" );
			}
		}
	}

	void Debug(object log)
	{
		if ( !skate_debug )
			return;
		Log.Info( log.ToString() );
	}
	void SimulateTopSpeed()
	{
		var top = TopSpeed;
		if ( GroundEntity == null )
			top = AirTopSpeed;
		if ( Velocity.Length > top )
			Velocity = Velocity.Normal * top;
	}
	void SimulateVert(ref Rotation RealRotation)
	{
		if ( Pawn is not SkatePawn skatePawn )
			return;
		var vertTraceLen = 100000f;
		var vertTraceInside = 0.0f;
		var vertInsideOffset = 2f;
		HasVertBelow = false;

		var vertTrace = Trace.Ray( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen) )
							.WorldAndEntities()
							.WithAnyTags( "vert" );
		if ( skate_debug )
			DebugOverlay.Line( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen), Color.Green );

		var vertResult = vertTrace.Run();
		if ( vertResult.Entity != null )
		{
			HasVertBelow = true;
			var vertNormal = (vertResult.Normal - vertResult.Normal.ProjectOnNormal( Vector3.Up )).Normal;
			/*
			if ( VertNormal != vertNormal )
			{*/
				var foundVert = true;
				var lastResult = vertResult;
				while(foundVert == true)
				{
					vertTraceInside -= 0.5f;
					vertTrace = Trace.Ray( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen) )
							.WorldAndEntities()
							.WithAnyTags( "vert" );
				
				var vertResult2 = vertTrace.Run();
				var vertNormal2 = (vertResult2.Normal - vertResult2.Normal.ProjectOnNormal( Vector3.Up )).Normal;
				if (vertResult2.Entity != null && vertNormal2 == vertNormal)
					{
					if ( skate_debug )
						DebugOverlay.Line( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen), Color.Yellow );
					foundVert = true;
					vertNormal = vertNormal2;
						lastResult = vertResult2;
					}
					else
					{
						foundVert = false;
						break;
					}
				}
				vertResult = lastResult;
				//var off = -RealRotation.Up * vertTraceInside;
				Position = new Vector3( vertResult.HitPosition.x, vertResult.HitPosition.y, Position.z );
				RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, vertNormal ) * RealRotation;
				//var normalDifference = (vertNormal - VertNormal).Normal;
				//Position -= normalDifference * vertInsideOffset;
				Position += vertNormal * vertInsideOffset;
			Position += RealRotation.Up * NudgeAmount;
				VertNormal = vertNormal;
				skatePawn.VertNormal = vertNormal;
			//}
		}
		Velocity -= Velocity.ProjectOnNormal( VertNormal );
	}
	public override void Simulate()
	{
		/*
		if ( broken )
			return;*/
		var skatePawn = Pawn as SkatePawn;
		skatePawn.TurnRight = -InputLD.DigitalLeftInput;
		skatePawn.OnVert = OnVert;
		var RealRotation = skatePawn.RealRotation;
		var stopped = Velocity.Length <= StoppedVelocity;
		var jump = false;
		
		if ( skate_debug )
		{
			DebugOverlay.Text( Velocity.Length.ToString(), Position );
			//DebugOverlay.Text( RealRotation.Yaw().ToString(), Position );
		}
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
		var falling = false;
		DoUpsideDownFall( ref RealRotation, ref falling );
		if ( falling )
			braking = false;
		skatePawn.Crouch = JumpReady;
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
		var vertTraceInside = 0.0f;
		var vertInsideOffset = 2f;
		HasVertBelow = false;
		if ( OnVert )
		{
			/*
			var vertTrace = Trace.Ray( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen) )
							.WorldAndEntities()
							.WithAnyTags( "vert" );
			if ( skate_debug )
				DebugOverlay.Line( Position + (RealRotation.Up * vertTraceInside), Position + (RealRotation.Up * vertTraceInside) - (Vector3.Up * vertTraceLen), Color.Green );

			var vertResult = vertTrace.Run();
			if ( vertResult.Entity != null )
			{
				HasVertBelow = true;
				var vertNormal = (vertResult.Normal - vertResult.Normal.ProjectOnNormal( Vector3.Up )).Normal;
				if ( VertNormal != vertNormal )
				{
					var off = -RealRotation.Up * vertTraceInside;
					Position = new Vector3( vertResult.HitPosition.x, vertResult.HitPosition.y, Position.z ) + off;
					RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, vertNormal ) * RealRotation;
					var normalDifference = (vertNormal - VertNormal).Normal;
					Position -= normalDifference * vertInsideOffset;
					//Position += vertNormal * vertInsideOffset;
					VertNormal = vertNormal;
					skatePawn.VertNormal = vertNormal;
				}
			}
			Velocity -= Velocity.ProjectOnNormal( VertNormal );*/
			SimulateVert(ref RealRotation);
		}

		if ( GroundEntity == null )
		{
			if (!OnVert)
				AngularVelocity = new Angles( (InputLD.DigitalForwardInput * AirPitchSpeed), (InputLD.DigitalLeftInput * AirSpinSpeed), 0f );
			else
				AngularVelocity = new Angles( 0f, (InputLD.DigitalLeftInput * AirSpinSpeed), 0f );
			Velocity += (Gravity * Vector3.Down) * Time.Delta;
			CalculateSpinTrick();
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

		if ( helper.TryMove( Time.Delta, false ) > 0 )
		{
			//TODO: ignore collisions when going up walkable stuff while still preventing the player from going oob. Maybe push away when inside walls in skatehelper?
			if ((helper.HitFloor || helper.Velocity.Length <= float.Epsilon) && GroundEntity == null)
			{
				if ( OnVert )
					NudgeAmount += 2f;
				else
					helper.Position += RealRotation.Up * 2f;
				helper.Velocity = Velocity;
				Debug( "nudge" );
			}
				if ( helper.HitWall)
				{
				if ( GroundEntity != null )
				{
					RealRotation = Rotation.LookAt( helper.Velocity.WithZ( 0f ), RealRotation.Up );
					RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, GroundNormal ) * RealRotation;
				}
					var velTowardsWall = Velocity.Dot( -helper.HitNormal );
					if (velTowardsWall >= HitForce)
					{
					if ( GroundEntity != null )
					{
						Rotation = RealRotation;
						SnapRotation = true;
						AddEvent( "angry" );
						//goofy
						if ( Vector3.Dot( RealRotation.Left, helper.HitNormal ) >= 0f )
							AddEvent( "back" );
						else
							AddEvent( "front" );
					}
						skatePawn.PlaySound( "body_hit" );
						if ( skatePawn.HasHelmet() )
							skatePawn.PlaySound( "helmet_hit" );
					}
				}
			Position = helper.Position;
			Velocity = helper.Velocity;
		}
		else
		{
			var trac = Trace.Ray( Position + Vector3.Up * 10f, Position + Vector3.Up * 10f + Velocity * Time.Delta )
				.WorldAndEntities()
				.WithAnyTags( "solid", "playerclip", "passbullets", "unskateable" );
			var sanityTrace = trac.Run();
			if (!sanityTrace.Hit)
				Position += Velocity * Time.Delta;
			else
				Position += RealRotation.Up * 2f;
		}

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
			if (dot > 0f)
				Velocity -= Velocity.ProjectOnNormal( heading );
					Debug( "sphere hit" );
			}
			if (skate_debug)
				DebugOverlay.Sphere( tracePos, wallCollisionSphereRadius, Color.Red );
		}
		var groundVector = Vector3.Up;
		if ( GroundEntity != null )
			groundVector = GroundNormal;

		var wasOnGround = GroundEntity != null;

		var currentTrace = 3f;
		
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
			ClearGroundEntity(ref RealRotation);
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
			if (canStand && GroundEntity != null)
			{
				var slopeDifference = floorResult.Normal.Angle( GroundNormal );
				/*
				if (slopeDifference != 0f)
					Log.Info( slopeDifference.ToString() );*/
				// > 0f means we're going down
				var dotFw = Vector3.Dot( floorResult.Normal, Velocity.Normal );

				if ( slopeDifference >= MaxSlope && dotFw > 0f )
				{
					canStand = false;
				}
			}
			if ( canStand )
			{
				var oldVelocity = Velocity;
				var oldForwardSpeed = Velocity.Dot( RealRotation.Forward );
				var prevGroundEnt = GroundEntity;
				UpdateGroundEntity( floorResult );
				var towardsFloorSpeed = Vector3.Dot( Velocity, -GroundNormal );
				if ( towardsFloorSpeed >= 100f && prevGroundEnt == null )
					AddEvent( "land" );
				
				var oldRotation = RealRotation;
				RealRotation = MathLD.FromToRotation( Vector3.Up * RealRotation, GroundNormal ) * RealRotation;
				if ( prevGroundEnt == null )
				{
					Debug( "landed" );
					var awkward = false;
					
					floorResult.Surface.DoFootstep( skatePawn, floorResult, 0, 20f );
					var bailed = false;
					var angleDifference = oldRotation.Up.Normal.Angle( floorResult.Normal );
					if ( angleDifference > LandBailMaxAngle )
					{
						awkward = true;
						Debug( "Landed awkwardly. (Angle: " + angleDifference + ")" );
						skatePawn.Bail(SkatePawn.BailType.Landing);
						bailed = true;
					}
					var sidewaysVelocity = Math.Abs( Vector3.Dot( Velocity, RealRotation.Right ) );
					if ( sidewaysVelocity >= SidewaysBailSpeed && !bailed )
					{
						Debug( "Landed sideways. (Velocity: " + sidewaysVelocity + ")" );
						skatePawn.Bail();
					}
					if ( Vector3.Dot( Velocity, RealRotation.Forward ) < 0 && Velocity.Length > StoppedVelocity )
					{
						RealRotation = Rotation.LookAt( RealRotation.Backward, GroundNormal );
					}
					if (!awkward)
						skatePawn.PlaySound( "skate_land" );
					SnapRotation = true;
					Rotation = RealRotation;
					oldForwardSpeed = oldVelocity.Dot( RealRotation.Forward );
					var ang = Math.Abs(GroundNormal.Angle( Vector3.Up ));
					oldForwardSpeed += ang * LandSpeedMultiplier;
				}
				else
				{
					Velocity = oldForwardSpeed * RealRotation.Forward;
				}
				RealRotation = Rotation.LookAt( RealRotation.Forward, GroundNormal );
				Velocity -= Velocity.ProjectOnNormal( floorResult.Normal );
				Position = floorResult.EndPosition + floorResult.Normal * 1f;
				GroundVert = isVert;
				skatePawn.TrickScores.Finished = true;
			}
			else
			{
				ClearGroundEntity(ref RealRotation);
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
			var jumpOffset = Vector3.Zero;
			var groundAngle = GroundNormal.Angle( Vector3.Up );
			var jumpDir = Vector3.Up;
			if ( groundAngle > 90f )
			{
				jumpDir *= -1;
				jumpOffset = jumpDir * 20f;
			}
			ClearGroundEntity(ref RealRotation);
			Velocity += jumpDir * JumpForce * currentJumpStrength;
			Position += jumpOffset;
			jumped = true;
			currentJumpStrength = JumpStrengthMinimum;
			AddEvent( "jump" );
			skatePawn.PlaySound( "ollie" );
		}

		if ( JumpReady )
		{
			if ( currentJumpStrength < 1f )
				currentJumpStrength += Time.Delta * JumpStrengthSpeed;
			else
				currentJumpStrength = 1f;
		}
		SimulateTopSpeed();
	}

	public override void FrameSimulate()
	{
		if ( Pawn is not SkatePawn skatePawn )
			return;
		var RealRotation = skatePawn.RealRotation;
		if (SnapRotation)
		{
			SnapRotation = false;
			Rotation = RealRotation;
		}
		else
			Rotation = Rotation.Lerp( Rotation, RealRotation, SmoothingSpeed * Time.Delta );
	}
}
