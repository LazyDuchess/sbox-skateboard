using Sandbox;

namespace Skateboard
{
	public class GameMusic : Entity
	{
		public Sound musicSound;
		public GameMusic()
		{
			musicSound = PlaySound( "skate_music" );
		}
	}
}
