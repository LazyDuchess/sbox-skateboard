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
			_name = "";
			_score = 0;
			_multiplier = 0;
		}
		public TrickScoreEntry(string name = "", int score = 0, int multiplier = 1)
		{
			_name = name;
			_score = score;
			_multiplier = multiplier;
		}
		[Net] public virtual int Score
		{
			get
			{
				return _score;
			}
			set
			{
				_score = value;
			}
		}
		[Net] public virtual int Multiplier
		{
			get
			{
				return _multiplier;
			}
			set
			{
				_multiplier = value;
			}
		}
		[Net] public virtual string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}
		private int _score;
		private int _multiplier;
		private string _name;
	}
}
