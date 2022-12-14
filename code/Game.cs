using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Skateboard;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class Game : Sandbox.Game
{
	[ConVar.Replicated]
	public static bool skate_sim_mode { get; set; } = false;
	public Game()
	{
		if ( IsServer )
		{
			Global.TickRate = 128;
			_ = new UI.SkateHUD();
		}
		/*
		else
			_ = new GameMusic();*/
	}

	void FixUpProps()
	{
		var ents = All.OfType<Prop>();
		foreach(var element in ents)
		{
			if ( element.PhysicsBody != null )
			{
				if ( element.PhysicsBody.MotionEnabled )
					element.Tags.Add( "dynamicprop" );
				else
					element.Tags.Add( "staticprop" );
			}
			else
			{
				element.Tags.Add( "staticprop" );
			}
		}
	}
	public override void PostLevelLoaded()
	{
		if ( IsServer )
			FixUpProps();
	}
	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new SkatePawn(client);
		client.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
			pawn.PostSpawn();
			//pawn.Spawn();
		}
	}
}
