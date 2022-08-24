using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Skateboard.Tricks
{
	public partial class TrickScoreEntry : BaseNetworkable
	{
		public TrickScoreEntry()
		{
			//networking is stupid and makes an empty one for some reason.
			Name = "";
			Score = 0;
			Multiplier = 0;
		}
		public TrickScoreEntry(string name = "", int score = 0, int multiplier = 1)
		{
			Name = name;
			Score = score;
			Multiplier = multiplier;
		}
		[Net] public int Score { get; set; }
		[Net] public int Multiplier { get; set; }
		[Net] public string Name { get; set; } = "";
	}
}
