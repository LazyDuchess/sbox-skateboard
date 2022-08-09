﻿using Sandbox;
using System;
using System.Linq;
using Skateboard.Cameras;

namespace Skateboard;

partial class SkatePawn : AnimatedEntity
{
	[Net, Predicted] public Rotation RealRotation { get; set; }
	[Net] public float BailTime { get; set; } = 3f;
	[Net] float timeBailed { get; set; } = 0f;
	[Net] bool bailed { get; set; } = false;
	/// <summary>
	/// Provides an easy way to switch our current cameramode component
	/// </summary>
	public CameraMode CameraMode
	{
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
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
		SetModel( "models/citizen/citizen.vmdl" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		Controller = new Player.SkateController();
		CameraMode = new SkateCamera();
		RealRotation = Rotation;
		timeBailed = 0f;
		bailed = false;
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
		ent.PhysicsEnabled = true;

		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" ) ) continue;
			if ( child is not ModelEntity e ) continue;

			var model = e.GetModelName();

			var clothing = new ModelEntity();
			clothing.SetModel( model );
			clothing.SetParent( ent, true );
			clothing.RenderColor = e.RenderColor;
			clothing.CopyBodyGroups( e );
			clothing.CopyMaterialGroup( e );
		}

		ent.DeleteAsync( BailTime );
	}

	public void Bail()
	{
		bailed = true;
		Particles.Create( "particles/impact.flesh.bloodpuff-big.vpcf", Position + Vector3.Up*20f );
		Particles.Create( "particles/impact.flesh-big.vpcf", Position + Vector3.Up * 20f );
		PlaySound( "kersplat" );

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

		Controller?.Simulate( cl, this, null );
		if ( bailed )
		{
			timeBailed += Time.Delta;

			if ( timeBailed >= BailTime )
			{
				Spawn();
			}
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );
		Controller?.FrameSimulate( cl, this, null );
		// Update rotation every frame, to keep things smooth
		
		//Rotation = Input.Rotation;
		//EyeRotation = Rotation;
	}
}
