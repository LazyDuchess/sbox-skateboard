using Sandbox;
using Sandbox.UI;

namespace Skateboard.UI
{
	public class SkateHUD : HudEntity<RootPanel>
	{
		public SkateHUD()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/ui/SkateboardHud.scss" );

			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<VoiceList>();
			RootPanel.AddChild<VoiceSpeaker>();
			RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
			RootPanel.AddChild<SkateScore>();
			RootPanel.AddChild<TrickScore>();
		}
	}
}
