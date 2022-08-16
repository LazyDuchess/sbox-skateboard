using Sandbox;
using System;
using System.Linq;
using Skateboard.Cameras;

namespace Skateboard;

partial class SkatePawn : AnimatedEntity
{
	public enum BailType
	{
		Normal,
		Landing
	}
	[Net] ModelEntity board { get; set; }
	/// <summary>
	/// The player animator is responsible for positioning/rotating the player and
	/// interacting with the animation graph.
	/// </summary>
	[Net, Predicted]
	public PawnAnimator Animator { get; set; }

	/// <summary>
	/// Return the controller to use. Remember any logic you use here needs to match
	/// on both client and server. This is called as an accessor every tick.. so maybe
	/// avoid creating new classes here or you're gonna be making a ton of garbage!
	/// </summary>
	public virtual PawnAnimator GetActiveAnimator() => Animator;

	public ModelEntity bailedEntity;
	/// <summary>
	/// The clothing container is what dresses the citizen
	/// </summary>
	public ClothingContainer Clothing = new();

	[Net, Predicted] public float TurnRight { get; set; } = 0f;

	[Net, Predicted] public bool Crouch { get; set; } = false;
	//For camera.
	[Net, Predicted] public bool OnVert { get; set; } = false;
	[Net, Predicted] public Vector3 VertNormal { get; set; } = Vector3.Zero;

	public SkatePawn()
	{
		Animator = new SkateAnimator(this);
	}

	public SkatePawn(Client cl) : this()
	{
		Clothing.LoadFromClient( cl );
	}

	[Net, Predicted] public Rotation RealRotation { get; set; }
	[Net] public float BailTime { get; set; } = 3f;
	[Net] float timeBailed { get; set; } = 0f;
	[Net] public bool bailed { get; set; } = false;
	/// <summary>
	/// Provides an easy way to switch our current cameramode component
	/// </summary>
	public CameraMode CameraMode
	{
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
	}

	public void Respawn()
	{
		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			Transform = tx;
			Spawn();
		}
	}

	[Net, Predicted]
	public Player.PawnController Controller { get; set; }
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();
		//
		// Use a watermelon model
		//
		SetModel( "models/skateanimations.vmdl" );
		//SetAnimGraph( "animgraphs/skateanimations.vanmgrph" );
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		Controller = new Player.SkateController();
		CameraMode = new SkateCamera();
		RealRotation = Rotation;
		timeBailed = 0f;
		bailed = false;
		Velocity = 0;
		AngularVelocity = Angles.Zero;
		Clothing.DressEntity( this );
		board = new ModelEntity( "models/skateboard_animated.vmdl", this );
		//board.Tags.Add( "board" );
	}

	public override void BuildInput( InputBuilder inputBuilder )
	{
		base.BuildInput( inputBuilder );
		Controller?.BuildInput( inputBuilder );
	}

	[ClientRpc]
	private void BecomeRagdollOnClient( Vector3 velocity )
	{
		var ent = new ModelEntity();
		ent.Tags.Add( "ragdoll", "solid", "debris" );
		ent.Position = Position;
		ent.Rotation = Rotation;
		ent.Scale = Scale;
		ent.UsePhysicsCollision = true;
		ent.EnableAllCollisions = true;
		ent.SetModel( GetModelName() );
		ent.CopyBonesFrom( this );
		ent.CopyBodyGroups( this );
		ent.CopyMaterialGroup( this );
		ent.CopyMaterialOverrides( this );
		ent.TakeDecalsFrom( this );
		ent.EnableAllCollisions = true;
		ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
		ent.RenderColor = RenderColor;
		ent.PhysicsGroup.Velocity = velocity;
		var angleVel = new Vector3( AngularVelocity.pitch, AngularVelocity.roll, AngularVelocity.yaw );
		//This adds some more variety to the ragdoll
		var vel = velocity.Length * 0.025f;
		var noise = 10f * vel;
		angleVel += new Vector3( Rand.Float( -noise, noise ), Rand.Float( -noise, noise ), Rand.Float( -noise, noise ) );
		ent.PhysicsGroup.AngularVelocity = angleVel;
		ent.PhysicsEnabled = true;

		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" )/* && !child.Tags.Has("board")*/) continue;
			if ( child is not ModelEntity e ) continue;

			var model = e.GetModelName();
			//var isBoard = child.Tags.Has( "board" );
			var clothing = new ModelEntity();
			clothing.SetModel( model );
			/*
			if ( !isBoard )
			{*/
				clothing.SetParent( ent, true );
				clothing.RenderColor = e.RenderColor;
				clothing.CopyBodyGroups( e );
				clothing.CopyMaterialGroup( e );
			/*}
			else
			{
				clothing.Position = Position;
				clothing.Rotation = Rotation;
				clothing.UsePhysicsCollision = true;
				clothing.EnableAllCollisions = true;
				clothing.SurroundingBoundsMode = SurroundingBoundsType.Physics;
				clothing.PhysicsGroup.Velocity = velocity;
				clothing.PhysicsEnabled = true;
			}*/
		}


		
		ent.DeleteAsync( BailTime );
		bailedEntity = ent;

		//Skateboard--------------------
		ent = new ModelEntity();
		ent.Tags.Add( "solid", "debris" );
		ent.Position = board.Position;
		ent.Rotation = board.Rotation;
		ent.Scale = board.Scale;
		ent.UsePhysicsCollision = true;
		ent.EnableAllCollisions = true;
		ent.SetModel( "models/skateboard.vmdl" );
		ent.CopyBonesFrom( board );
		ent.CopyBodyGroups( board );
		ent.CopyMaterialGroup( board );
		ent.CopyMaterialOverrides( board );
		ent.TakeDecalsFrom( board );
		ent.EnableAllCollisions = true;
		ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
		ent.RenderColor = board.RenderColor;
		ent.PhysicsGroup.Velocity = Velocity;
		ent.PhysicsGroup.AngularVelocity = new Vector3( AngularVelocity.pitch, AngularVelocity.roll, AngularVelocity.yaw );
		ent.PhysicsEnabled = true;
		ent.DeleteAsync( BailTime );
		//--------------------------------
	}

	public void Bail(BailType bailType = BailType.Normal)
	{
		if ( bailed )
			return;
		bailed = true;
		if ( bailType == BailType.Landing )
		{
			//Particles.Create( "particles/impact.flesh.bloodpuff-big.vpcf", Position + Vector3.Up * 20f );
			Particles.Create( "particles/impact.flesh-big.vpcf", Position + Rotation.Up * 50f );
			PlaySound( "kersplat" );
		}
		BecomeRagdollOnClient( Velocity );
		Controller = null;

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children )
		{
			child.EnableDrawing = false;
		}
		bailed = true;
		timeBailed = 0f;
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( Client cl )
	{
		base.Simulate( cl );
		GetActiveAnimator()?.Simulate();
		Controller?.Simulate( cl, this, GetActiveAnimator() );
		if ( bailed )
		{
			timeBailed += Time.Delta;

			if ( timeBailed >= BailTime )
			{
				Spawn();
			}
		}
		if ( Input.Pressed( InputButton.Reload ) && !bailed)
			Respawn();
		if ( Input.Pressed( InputButton.Flashlight ) && !bailed )
			Bail();
		
	}/*
	void MirrorBones()
	{
		var boneCount = BoneCount;
		var myLocalPos = Position;
		var myLocalRot = Rotation.Inverse;
		for (var i=0;i<1;i++ )
		{
			var parent = GetBoneParent( i );
			var localPos = myLocalPos;
			var localRot = myLocalRot;
			var localBone = GetBoneTransform( i, false );
				localBone.Position -= localPos;
				localBone.Position *= localRot;
				localBone.Rotation = (localRot * localBone.Rotation);
			myLocalPos = Position;
			myLocalRot = Rotation.Inverse;
		}
		
	}*/
	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );
		Controller?.FrameSimulate( cl, this, GetActiveAnimator());

		// Update rotation every frame, to keep things smooth

		//Rotation = Input.Rotation;
		//EyeRotation = Rotation;
	}
}
