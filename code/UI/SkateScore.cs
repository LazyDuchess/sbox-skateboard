using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skateboard.UI
{
	public class SkateScore : Panel
	{
		Label scoreLabel;
		public SkateScore()
		{
			StyleSheet.Load( "/ui/SkateScore.scss" );
			scoreLabel = Add.Label( "Score: 0", "score text" );
		}
		public override void Tick()
		{
			base.Tick();
			var skatePawn = Local.Pawn as SkatePawn;
			var trickHolder = skatePawn.TrickScores;
			scoreLabel.Text = "Score: " + trickHolder.TotalScore.ToString();
		}
	}
}
