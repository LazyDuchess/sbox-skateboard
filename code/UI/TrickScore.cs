using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skateboard.UI
{
	public class TrickScore : Panel
	{
		private Label multiplierLabel;
		private Label trickLabel;
		public TrickScore()
		{
			StyleSheet.Load( "/ui/TrickScore.scss" );
			multiplierLabel = Add.Label( "100 x 1", "text multiplier" );
			trickLabel = Add.Label( "Kickflip + Manual + FS 50-50 + 180 Ollie + Hardflip + The 900", "text tricks" );
			multiplierLabel.SetClass( "multiplier done", false );
			trickLabel.SetClass( "tricks", false );
		}

		public override void Tick()
		{
			base.Tick();
			var skatePawn = Local.Pawn as SkatePawn;
			var trickHolder = skatePawn.TrickScores;
			if ( trickHolder.Empty )
			{
				multiplierLabel.SetClass( "multiplier", false );
				trickLabel.SetClass( "tricks", false );
				multiplierLabel.Text = "";
				trickLabel.Text = "";
			}
			else
			{
				multiplierLabel.SetClass( "multiplier", true );
				multiplierLabel.SetClass( "done", trickHolder.Finished );
				multiplierLabel.SetClass( "failed", trickHolder.Failed );
				trickLabel.SetClass( "tricks", true );
				trickLabel.SetClass( "failed", trickHolder.Failed );
				multiplierLabel.Text = trickHolder.Score + " x " + trickHolder.Multiplier;
				trickLabel.Text = trickHolder.String;
			}
		}
	}
}
